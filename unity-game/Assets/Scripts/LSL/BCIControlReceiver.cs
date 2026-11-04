using System;
using System.Collections;
using LSL;
using UnityEngine;

/// <summary>
/// Recebe o stream motor contínuo produzido pelo controlador Python.
///
/// Stream padrão:
///   name = GrazMI_Control
///   type = BCIControl
///
/// Canais atuais (quando disponíveis):
///   [0] left_leg
///   [1] right_leg
///   [2] rest
///   [3] left
///   [4] both          (legado/diagnóstico)
///   [5] right
///   [6] confidence
///   [7] density_gate
///   [8] state_id
///
/// Somente left_leg/right_leg são necessários para o movimento no Unity.
/// Isso desacopla o avatar das classes internas do decoder e torna o Unity
/// compatível tanto com single_target quanto com two_legs.
/// </summary>
public sealed class BCIControlReceiver : MonoBehaviour
{
    [Header("BCI Control LSL")]
    [SerializeField] private string streamName = "GrazMI_Control";
    [SerializeField] private string streamType = "BCIControl";
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;

    private StreamInlet inlet;
    private float[] sample;
    private Coroutine resolveRoutine;

    public bool IsConnected => inlet != null;
    public string Status { get; private set; } = "aguardando";

    public float LastLeftLeg { get; private set; }
    public float LastRightLeg { get; private set; }
    public float LastRest { get; private set; }
    public float LastLeft { get; private set; }
    public float LastBoth { get; private set; }
    public float LastRight { get; private set; }
    public float LastConfidence { get; private set; }
    public float LastDensityGate { get; private set; }
    public int LastStateId { get; private set; }

    public event Action<float, float, float, float, float, float, float, float, int, double> SampleReceived;
    public event Action<string> StatusChanged;

    public void Configure(string newName, string newType)
    {
        streamName = string.IsNullOrWhiteSpace(newName) ? "GrazMI_Control" : newName.Trim();
        streamType = string.IsNullOrWhiteSpace(newType) ? "BCIControl" : newType.Trim();
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

                if (info.channel_count() < 2)
                {
                    SetStatus($"{info.name()}: {info.channel_count()} canais; esperado >= 2");
                    yield return new WaitForSecondsRealtime(retryInterval);
                    continue;
                }

                inlet = new StreamInlet(info);
                sample = new float[info.channel_count()];
                SetStatus($"conectado a {info.name()}");
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

            LastLeftLeg     = Mathf.Clamp01(sample[0]);
            LastRightLeg    = Mathf.Clamp01(sample[1]);
            LastRest        = sample.Length > 2 ? Mathf.Clamp01(sample[2]) : 0f;
            LastLeft        = sample.Length > 3 ? Mathf.Clamp01(sample[3]) : 0f;
            LastBoth        = sample.Length > 4 ? Mathf.Clamp01(sample[4]) : 0f;
            LastRight       = sample.Length > 5 ? Mathf.Clamp01(sample[5]) : 0f;
            LastConfidence  = sample.Length > 6 ? Mathf.Clamp01(sample[6]) : 0f;
            LastDensityGate = sample.Length > 7 ? sample[7] : 0f;
            LastStateId     = sample.Length > 8 ? Mathf.RoundToInt(sample[8]) : 0;

            SampleReceived?.Invoke(
                LastLeftLeg,
                LastRightLeg,
                LastRest,
                LastLeft,
                LastBoth,
                LastRight,
                LastConfidence,
                LastDensityGate,
                LastStateId,
                timestamp);
        }
        while (timestamp != 0.0);
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
