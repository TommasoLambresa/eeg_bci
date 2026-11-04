using System;
using System.Collections;
using LSL;
using UnityEngine;

/// <summary>
/// Recebe diretamente os marcadores do PsychoPy Graz existente.
///
/// Stream esperado:
/// name = GrazMI_Markers
/// type = Markers
///
/// Códigos:
/// 1 BASELINE
/// 2 ATTENTION
/// 3 LEFT_MI_STIM
/// 4 RIGHT_MI_STIM
/// 5 ATTEMPT
/// 6 REST
/// 99 BLOCK_END
/// </summary>
public sealed class PsychoPyMarkerReceiver : MonoBehaviour
{
    [Header("PsychoPy marker stream")]
    [SerializeField] private string streamName = "GrazMI_Markers";
    [SerializeField] private string streamType = "Markers";
    [SerializeField, Min(0.2f)] private float retryInterval = 1f;

    private StreamInlet inlet;
    private readonly float[] sample = new float[1];
    private Coroutine resolveRoutine;

    public bool IsConnected => inlet != null;
    public string Status { get; private set; } = "aguardando";

    public event Action<int, double> MarkerReceived;
    public event Action<string> StatusChanged;

    public void Configure(string newName, string newType)
    {
        streamName = string.IsNullOrWhiteSpace(newName) ? "GrazMI_Markers" : newName.Trim();
        streamType = string.IsNullOrWhiteSpace(newType) ? "Markers" : newType.Trim();
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

        SetStatus("desconectado");
    }

    private IEnumerator ResolveStream()
    {
        while (inlet == null)
        {
            SetStatus($"procurando {streamName}");

            // O PsychoPy tem nome fixo. Não usamos fallback amplo por "Markers"
            // para evitar conectar por engano ao outlet de feedback do próprio Unity.
            StreamInfo[] streams = LSL.LSL.resolve_stream("name", streamName, 1, 0.25);

            if (streams != null && streams.Length > 0)
            {
                StreamInfo info = streams[0];
                inlet = new StreamInlet(info);
                SetStatus($"conectado a {info.name()}");
                break;
            }

            yield return new WaitForSecondsRealtime(retryInterval);
        }

        resolveRoutine = null;
    }

    private void Update()
    {
        if (inlet == null)
            return;

        double timestamp;
        do
        {
            timestamp = inlet.pull_sample(sample, 0.0f);

            if (timestamp != 0.0)
                MarkerReceived?.Invoke(Mathf.RoundToInt(sample[0]), timestamp);
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
