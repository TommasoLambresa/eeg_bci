using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Esse Script serve para controlar o protocolo GRAZ
public class GrazController : MonoBehaviour
{
    #region SINGLETON

    private static GrazController _instance;

    public static GrazController instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GrazController>();
            return _instance;
        }
    }

    #endregion

    [SerializeField] private CanvasGroup _group;

    [Header("Baseline Phase")]
    public float baselineDuration = 10f; // Duração da baseline

    [Header("Fixation Phase")]
    public float fixationDuration = 3f; // Duração da fixação
    public RawImage indicationImage; // Imagem de indicação

    [Header("Cue Phase")]
    public float cueDuration = 1f; // Duração do cue
    public RawImage cueImageDirection; // Imagem de direção
    public Texture defaultCueImage; // Imagem padrão

    [Header("Feedback Phase")]
    public float feedbackDuration = 3.75f; // Duração do feedback
    [HideInInspector] public bool OnFeedbackPhase = false; // Flag de fase de feedback

    [Header("Pause Phase")]
    public float minPauseDuration = 1.5f; // Pausa mínima
    public float maxPauseDuration = 3.5f; // Pausa máxima

    [HideInInspector] public float grazInterval;
    [HideInInspector] public bool inGrazProtocol;

    private AudioSource source;

    private Coroutine grazCoroutine;

    private RealtimeLog realtimeLog; // Sistema de log
    private StartGame startGame; // Controlador do jogo

    private void Awake()
    {
        if (TryGetComponent(out AudioSource s))
            source = s;
        else
            source = gameObject.AddComponent<AudioSource>();

        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        ToggleCanvas(false);
    }

    private void Start()
    {
        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;
        realtimeLog._acerto = "NEUTRO";
    }

    // Ativa/desativa canvas
    public void ToggleCanvas(bool on)
    {
        _group.alpha = on == true ? _group.alpha = 1f : _group.alpha = 0f;
        _group.interactable = on;
    }

    public void StartProtocol() => grazCoroutine = StartCoroutine(GrazProtocol());

    // Define rotação da imagem de cue
    public void SetCueImageDirection(float dir)
    {
        cueImageDirection.rectTransform.eulerAngles = new Vector3(cueImageDirection.rectTransform.rotation.x, cueImageDirection.rectTransform.rotation.y, dir);
    }

    public void EndProtocol()
    {
        if (grazCoroutine == null)
            return;
        StopCoroutine(grazCoroutine);
    }

    // Protocolo principal Graz
    private IEnumerator GrazProtocol()
    {
        Debug.Log($"Protocolo Graz iniciado");

        if (startGame == null)
            startGame = StartGame.instance;

        int trials = startGame.trials;

        yield return BaselinePhase();

        realtimeLog._acerto = "NEUTRO";

        inGrazProtocol = true;

        int cueCount = 0;

        var cueConfigs = UIConfigs.instance.cueConfigs;

        // Loop por todas as configurações de cue
        while (cueCount < UIConfigs.instance.cueCount)
        {
            realtimeLog._classe = cueCount;
            var config = cueConfigs[cueCount].GetComponent<CueConfigs>();
            cueDuration = string.IsNullOrEmpty(config.field.text) ? 5f : float.Parse(config.field.text);
            config.SetCueImage();

            UIConfigs.instance.SetLegType(config.dropdown);

            // Configura target baseado no tipo de UI
            if (UIConfigs.instance._ui == UIModo.Dois)
            {
                if (UIConfigs.instance._legType == LegType.LEFT)
                {
                    realtimeLog._target = "ESQUERDA";
                    SetCueImageDirection(180f);
                }
                else
                {
                    realtimeLog._target = "DIREITA";
                    SetCueImageDirection(0f);
                }
            }

            // Loop por todos os trials
            while (startGame.actTrials < trials && StartGame.instance.Started)
            {
                realtimeLog._tentativa = startGame.actTrials;
                Debug.Log($"Trial {StartGame.instance.actTrials} / {trials}");

                yield return FixationPhase();

                yield return CuePhase(cueCount);

                yield return FeedbackPhase();
                realtimeLog._acerto = "NEUTRO";
                yield return PausePhase();

                startGame.actTrials++;
            }

            cueCount++;
            StartGame.instance.actTrials = 0;
        }

        inGrazProtocol = false;

        Debug.Log($"Protocolo Graz concluido");

        StartGame.instance.RestartGame();
    }

    public IEnumerator BaselinePhase()
    {
        Debug.Log("Inicio da baseline");

        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;

        realtimeLog._marcador = "BASE";
        realtimeLog._marcadorNum = 1;

        yield return new WaitForSeconds(baselineDuration);

        Debug.Log("Fim da baseline");
    }

    public IEnumerator FixationPhase()
    {
        Debug.Log("Inicio do fixation");

        indicationImage.enabled = true;

        realtimeLog._marcador = "FIX";
        realtimeLog._marcadorNum = 2;

        yield return new WaitForSeconds(fixationDuration);

        Debug.Log("Fim do fixation");
    }

    public IEnumerator CuePhase(int cue)
    {
        Debug.Log("Inicio do cue");

        cueImageDirection.enabled = true;
        indicationImage.enabled = false;

        // Lança bola se for experiencia 1
        if (BallLauncher.instance != null)
            BallLauncher.instance.LaunchSingleBall();

        realtimeLog._marcador = "CUE";
        realtimeLog._marcadorNum = 3;

        yield return new WaitForSeconds(cueDuration);

        cueImageDirection.enabled = false;

        Debug.Log("Fim do cue");
    }

    private float randomDuration;

    public IEnumerator FeedbackPhase()
    {
        Debug.Log("Inicio do feedback");

        OnFeedbackPhase = true;

        realtimeLog._marcador = "FEED";
        realtimeLog._marcadorNum = 4;

        yield return new WaitForSeconds(feedbackDuration);

        OnFeedbackPhase = false;

        // Processa resultado para experiencia 2
        if (StartGame.instance.name == "Iniciar Game Dois")
        {
            var medidor = UIConfigs.instance.statusUI.medidor.GetComponentInChildren<MedidorMovement>();

            if (medidor.entrouOver)
            {
                medidor.ConfirmOverlapping();
                medidor.entrouOver = false;
                FeedbackController.instance.PlayErrou();
            }
        }

        Debug.Log("Fim do feedback");
    }

    public IEnumerator PausePhase()
    {
        Debug.Log("Inicio do pause");

        realtimeLog._marcador = "PAUSE";
        realtimeLog._marcadorNum = 5;

        yield return new WaitForSeconds(randomDuration);

        Debug.Log("Fim do pause");
    }
}
