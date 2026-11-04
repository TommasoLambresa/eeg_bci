using UnityEngine;
using TMPro;
using LSL;
using System.Collections;

public class LSLReceiver : MonoBehaviour
{
    private static LSLReceiver _instance;

    public static LSLReceiver instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LSLReceiver>();
            return _instance;
        }
    }

    [Header("Configurações LSL")]
    public string streamName = "Python_EEG";
    public string streamType = "EEG";

    private StreamInfo[] streamInfos;
    private StreamInlet inlet;
    private float[] sample;
    private int channelCount;
    private float sampleRate;

    [Space, Header("Painel")]
    public TMP_Text debugConection;
    private Animator debugAnimator;

    [Space, Header("Player")]
    public PlayerController _player;

    [Space, Header("Diagnóstico LSL")]
    [SerializeField] private bool connected;
    [SerializeField] private int samplesReceived;
    [SerializeField] private float lastRight;
    [SerializeField] private float lastBoth;
    [SerializeField] private float lastLeft;

    private bool warnedAboutChannels;

    private void Start()
    {
        if (debugConection != null)
            debugAnimator = debugConection.GetComponentInParent<Animator>();

        if (_player == null)
            _player = PlayerController.instance;
    }

    public void CallInitialize(UIConfigs configs)
    {
        StartCoroutine(Initialize(configs));
    }

    private IEnumerator Initialize(UIConfigs configs)
    {
        if (debugAnimator != null)
            debugAnimator.Play("Show_debug");

        yield return StartCoroutine(ResolveStream());

        if (streamInfos != null && streamInfos.Length > 0)
        {
            inlet = new StreamInlet(streamInfos[0]);

            channelCount = inlet.info().channel_count();
            sampleRate = (float)inlet.info().nominal_srate();
            sample = new float[channelCount];
            connected = true;

            if (debugConection != null)
            {
                debugConection.text =
                    $"Conectado a {streamName} - {channelCount} canais a {sampleRate}Hz";
                debugConection.color = Color.green;
            }

            Debug.Log(
                $"[LSLReceiver] Conectado: {streamName} | " +
                $"{channelCount} canais | {sampleRate} Hz"
            );
        }
        else
        {
            connected = false;

            if (debugConection != null)
            {
                debugConection.text =
                    "LSL não encontrado. Entrando em modo offline...";
                debugConection.color = Color.yellow;
            }

            Debug.LogWarning($"[LSLReceiver] Stream {streamName} não encontrado.");
        }

        if (configs != null)
            configs.SetarConfigs();

        yield return new WaitForSeconds(1.5f);

        if (debugAnimator != null)
            debugAnimator.Play("Hide_debug");
    }

    private IEnumerator ResolveStream()
    {
        streamInfos = null;

        float timeout = 5f;
        float startTime = Time.time;

        if (debugConection != null)
        {
            debugConection.text = $"Procurando stream {streamName}...";
            debugConection.color = new Color32(207, 125, 0, 255);
        }

        while (streamInfos == null || streamInfos.Length == 0)
        {
            streamInfos = LSL.LSL.resolve_stream(
                "name",
                streamName,
                1,
                1.0
            );

            if (Time.time - startTime > timeout)
            {
                streamInfos = null;

                if (debugConection != null)
                {
                    debugConection.text =
                        "Stream não encontrado dentro do tempo limite!";
                    debugConection.color = Color.red;
                }

                yield return new WaitForSeconds(1f);

                if (debugAnimator != null)
                    debugAnimator.Play("Hide_debug");

                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (inlet == null || sample == null)
            return;

        // Esvazia a fila e usa somente a amostra mais recente.
        // Assim um stream de 128 Hz não acumula atraso em um jogo a ~60 FPS.
        bool received = false;

        while (inlet.pull_sample(sample, 0.0f) != 0.0)
        {
            received = true;
            samplesReceived++;
        }

        if (received)
            ProcessData(sample);
    }

    private void ProcessData(float[] data)
    {
        if (data == null || data.Length < 3)
        {
            if (!warnedAboutChannels)
            {
                Debug.LogWarning(
                    "[LSLReceiver] Python_EEG precisa ter pelo menos 3 canais: " +
                    "direita, ambas, esquerda."
                );
                warnedAboutChannels = true;
            }

            return;
        }

        lastRight = data[0];
        lastBoth = data[1];
        lastLeft = data[2];

        if (_player == null)
            _player = PlayerController.instance;

        if (_player == null)
        {
            Debug.LogWarning("[LSLReceiver] PlayerController não encontrado.");
            return;
        }

        // IMPORTANTE:
        // Não depende mais de StartGame.GetStats(), Graz ou Feedback.
        // Os comandos LSL podem mover as pernas a qualquer instante.
        _player.UpdateLegValues(lastRight, lastBoth, lastLeft);
    }

    public void StopInlet()
    {
        connected = false;

        if (inlet != null)
            inlet.close_stream();
    }
}