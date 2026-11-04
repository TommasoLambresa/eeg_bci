using System;
using System.Collections;
using LSL;
using UnityEngine;

/// <summary>
/// Recebe a saída contínua do decoder Python.
///
/// Stream padrão:
///   name = Signal
///   type = BCI
///
/// Contrato v3.1 (canais mínimos):
///   [0] rep1
///   [1] rep2
///   [2] p_left
///   [3] p_rest
///   [4] p_right
///
/// Canais opcionais, recomendados para modelos single_target:
///   [5] active_left
///   [6] active_rest
///   [7] active_right
///
/// predicted_class/confidence são calculados no Unity a partir das probabilidades;
/// portanto o Python não precisa duplicar essa informação no stream.
///
/// rep1/rep2 continuam disponíveis para dashboard/log/análise, mas o Unity não
/// precisa mais desenhar a posição PCA. As probabilidades são usadas apenas no
/// feedback visual; o movimento das pernas continua vindo de GrazMI_Control.
/// </summary>
public sealed class BCIDecoderReceiver : MonoBehaviour
{
    [Header("Decoder LSL")]
    [SerializeField] private string streamName = "Signal";
    [SerializeField] private string streamType = "BCI";
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;

    private StreamInlet inlet;
    private float[] sample;
    private Coroutine resolveRoutine;

    public bool IsConnected => inlet != null;
    public string Status { get; private set; } = "aguardando";
    public int ChannelCount => sample != null ? sample.Length : 0;

    public float LastRep1 { get; private set; }
    public float LastRep2 { get; private set; }
    public float LastPca1 => LastRep1;
    public float LastPca2 => LastRep2;

    public float LastLeft  { get; private set; }
    public float LastRest  { get; private set; }
    public float LastRight { get; private set; }

    // Alias legado para scripts antigos. No protocolo atual o canal central é REST.
    public float LastBoth => LastRest;

    public bool ActiveLeft  { get; private set; } = true;
    public bool ActiveRest  { get; private set; } = true;
    public bool ActiveRight { get; private set; } = true;

    public int LastPredictedClassId { get; private set; } = -1;
    public float LastConfidence { get; private set; }

    // rep1, rep2, pLeft, pRest, pRight, timestamp
    public event Action<float, float, float, float, float, double> SampleReceived;
    public event Action<string> StatusChanged;

    public void Configure(string newName, string newType)
    {
        streamName = string.IsNullOrWhiteSpace(newName) ? "Signal" : newName.Trim();
        streamType = string.IsNullOrWhiteSpace(newType) ? "BCI" : newType.Trim();
    }

    public void StartReceiving()
    {
        if (inlet != null || resolveRoutine != null)
            return;

        resolveRoutine = StartCoroutine(ResolveStream());
    }

    public void StopReceiving()
    {
        if (resolveRoutine != null)
        {
            StopCoroutine(resolveRoutine);
            resolveRoutine = null;
        }

        if (inlet != null)
        {
            inlet.close_stream();
            inlet = null;
        }

        sample = null;
        SetStatus("desconectado");
    }

    private IEnumerator ResolveStream()
    {
        while (inlet == null)
        {
            SetStatus($"procurando {streamName}");

            StreamInfo[] streams = LSL.LSL.resolve_stream("name", streamName, 1, 0.25);

            if ((streams == null || streams.Length == 0) &&
                string.IsNullOrWhiteSpace(streamName) &&
                !string.IsNullOrWhiteSpace(streamType))
            {
                streams = LSL.LSL.resolve_stream("type", streamType, 1, 0.25);
            }

            if (streams != null && streams.Length > 0)
            {
                StreamInfo info = streams[0];

                if (info.channel_count() < 5)
                {
                    SetStatus($"{info.name()}: {info.channel_count()} canais; esperado >= 5");
                    yield return new WaitForSecondsRealtime(retryInterval);
                    continue;
                }

                inlet = new StreamInlet(info);
                sample = new float[info.channel_count()];
                SetStatus($"conectado a {info.name()} ({info.channel_count()} ch)");
                break;
            }

            yield return new WaitForSecondsRealtime(retryInterval);
        }

        resolveRoutine = null;
    }

    private void Update()
    {
        if (inlet == null || sample == null)
            return;

        double timestamp;
        do
        {
            timestamp = inlet.pull_sample(sample, 0.0f);
            if (timestamp == 0.0)
                continue;

            LastRep1  = sample[0];
            LastRep2  = sample[1];
            LastLeft  = Mathf.Clamp01(sample[2]);
            LastRest  = Mathf.Clamp01(sample[3]);
            LastRight = Mathf.Clamp01(sample[4]);

            // Sem flags explícitas, considera as três barras disponíveis. Isso
            // permite um contrato mínimo de 5 canais. Para single_target, os
            // flags 5..7 permitem deixar visualmente inativa a classe ausente.
            ActiveLeft  = sample.Length > 5 ? sample[5] > 0.5f : true;
            ActiveRest  = sample.Length > 6 ? sample[6] > 0.5f : true;
            ActiveRight = sample.Length > 7 ? sample[7] > 0.5f : true;

            LastPredictedClassId = ComputePredictedClass();
            LastConfidence = ComputeConfidence();

            SampleReceived?.Invoke(
                LastRep1,
                LastRep2,
                LastLeft,
                LastRest,
                LastRight,
                timestamp);
        }
        while (timestamp != 0.0);
    }

    private int ComputePredictedClass()
    {
        float best = float.NegativeInfinity;
        int id = -1;

        if (ActiveRest && LastRest > best)
        {
            best = LastRest;
            id = 0;
        }
        if (ActiveLeft && LastLeft > best)
        {
            best = LastLeft;
            id = 1;
        }
        if (ActiveRight && LastRight > best)
        {
            id = 2;
        }

        return id;
    }

    private float ComputeConfidence()
    {
        float best = 0f;
        if (ActiveLeft)  best = Mathf.Max(best, LastLeft);
        if (ActiveRest)  best = Mathf.Max(best, LastRest);
        if (ActiveRight) best = Mathf.Max(best, LastRight);
        return best;
    }

    private void SetStatus(string value)
    {
        Status = value;
        StatusChanged?.Invoke(value);
    }

    private void OnDestroy()
    {
        StopReceiving();
    }
}
