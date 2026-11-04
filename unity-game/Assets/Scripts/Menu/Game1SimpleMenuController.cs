using System;
using System.Collections;
using System.IO;
using System.Linq;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Integração do Game 1 com o pipeline BCI já existente.
///
/// O PsychoPy NÃO é modificado.
/// - GrazMI_Markers determina as fases e a condição visual da tentativa.
/// - LEFT_MI_STIM (3) -> esquerda; RIGHT_MI_STIM (4) -> direita; REST_STIM (8) -> centro; REST (6) é fase.
/// - Signal/BCI mantém rep1/rep2 e envia predict_proba LEFT/REST/RIGHT para feedback visual; NÃO move as pernas.
/// - GrazMI_Control/BCIControl fornece left_leg/right_leg e controla o avatar continuamente nos blocos online.
/// - TREINO ignora o BCI: cue espacial no estímulo e movimento demonstrativo no ATTEMPT.
/// - Cue/bola e predição BCI são independentes: um distrator pode lançar uma bola sem mover a perna.
/// - O mesmo Unity atende single_target e two_legs; a lógica de decisão fica no Python.
/// - O bloco com feedback mostra probabilidades contínuas; o PCA permanece disponível no stream/log.
/// </summary>
public enum Game1FeedbackMode
{
    AvatarAndBalls = 0,
    AvatarRepresentationAndBalls = 1,
    AvatarRepresentationCue = 2,
    AvatarCueTraining = 3
}

// Mantemos o nome do enum para compatibilidade de serialização com as cenas
// já existentes. Ele agora representa os três tipos de bloco do Game 1.
public enum Game1OnlineBlock
{
    IM_online_sem_feedback = 0,
    IM_online_PCA = 1,
    Treino = 2
}

public sealed class Game1SimpleMenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuRoot;
    [Tooltip("Subject ID, por exemplo LR102.")]
    public TMP_InputField participantInput;
    [Tooltip("Sessão: aceita 1, 2, 3... ou S1, S2, S3...")]
    public TMP_InputField sessionInput;
    public TMP_Text outputPathText;
    public TMP_Text eegStatusText;
    public TMP_Text markerStatusText;
    public Button chooseFolderButton;

    [Header("Bloco / organização de dados")]
    public Game1OnlineBlock onlineBlock = Game1OnlineBlock.Treino;
    public Button onlineBlockButton;
    public TMP_Text onlineBlockButtonText;
    [Tooltip("A pasta selecionada na interface é somente a raiz dos dados, ex.: C:\\Users\\User\\Desktop\\Dados")]
    public string experimentType = "BMI";
    public string onlineFolderName = "online";

    public Button cameraAdjustButton;
    public Button startButton;
    public Button backButton;

    [Header("Modo de feedback")]
    public Game1FeedbackMode feedbackMode = Game1FeedbackMode.AvatarAndBalls;
    public Button feedbackModeButton;
    public TMP_Text feedbackModeButtonText;
    public Button chooseRepresentationMapButton;
    public TMP_Text representationMapPathText;
    public RepresentationFeedbackView representationView; // legado: não exibido no protocolo v3.1
    public ClassifierProbabilityView classifierFeedbackView;

    [Header("Layout do feedback do classificador")]
    public Vector2 classifierFeedbackPosition = new Vector2(0f, -72f);
    public Vector2 classifierFeedbackSize = new Vector2(470f, 390f);

    [Header("Indicador espacial do PsychoPy")]
    [Tooltip("Overlay visual usado no TREINO e no bloco IM_online_PCA.")]
    public GameObject cueCommandRoot;
    [Tooltip("Símbolo espacial: círculo vazio no planejamento e cheio na execução.")]
    public TMP_Text cueCommandText;
    [Tooltip("Rótulo de fase: PREPARE / AGORA. Não informa esquerda/direita por texto.")]
    public TMP_Text cuePhaseText;

    [Header("Posições do indicador espacial")]
    [Tooltip("Posição do círculo sobre a perna esquerda, relativa ao centro da tela.")]
    public Vector2 cueLeftPosition = new Vector2(-170f, -155f);
    [Tooltip("Posição central usada exclusivamente pelo estímulo visual REST_STIM (marker 8).")]
    public Vector2 cueCenterPosition = new Vector2(0f, -155f);
    [Tooltip("Posição do círculo sobre a perna direita, relativa ao centro da tela.")]
    public Vector2 cueRightPosition = new Vector2(170f, -155f);
    public float cuePlanningFontSize = 96f;
    public float cueExecutionFontSize = 78f;

    [Header("Layout do mapa de representação")]
    [Tooltip("Posição do mapa no modo AVATAR + MAPA + BOLAS. O tamanho/aspecto é igual ao modo com comandos.")]
    public Vector2 representationMapPositionWithBalls = new Vector2(-36f, -14f);

    [Tooltip("Posição do mapa no modo AVATAR + MAPA + COMANDO.")]
    public Vector2 representationMapPositionWithCommands = new Vector2(0f, -14f);

    [Tooltip("Mesmo tamanho/aspecto do mapa em todos os modos que usam representação.")]
    public Vector2 representationMapSize = new Vector2(700f, 300f);

    [Header("Compatibilidade de layout antigo")]
    [Tooltip("Legado: não é mais usado pelo indicador espacial.")]
    public Vector2 cueCommandPosition = new Vector2(0f, 55f);
    [Tooltip("Legado: não é mais usado pelo indicador espacial.")]
    public Vector2 cueCommandSize = new Vector2(700f, 76f);

    [Header("Placar do Game 1")]
    public GameObject scoreRoot;
    public TMP_Text scoreText;

    [Header("Ajuste de câmera")]
    public GameObject cameraAdjustRoot;
    public TMP_Text cameraValuesText;
    public Slider cameraFovSlider;
    public Button cameraDoneButton;

    [Header("Câmera padrão do Game 1")]
    public bool applyDefaultCameraView = true;
    [Range(29f, 69f)] public float defaultCameraRotation = 45f;
    [Range(0f, 1f)] public float defaultCameraHorizontal = 0.51f;
    [Range(0f, 1f)] public float defaultCameraVertical = 0.54f;
    [Range(40f, 100f)] public float defaultCameraFov = 76f;
    [Range(30f, 100f)] public float minCameraFov = 45f;
    [Range(40f, 120f)] public float maxCameraFov = 95f;

    [SerializeField] private float mainCameraZMin = -0.5f;
    [SerializeField] private float mainCameraZMax = 0.7f;
    [SerializeField] private float mainCameraYMin = -1.0f;
    [SerializeField] private float mainCameraYMax = 0.0f;

    [Header("Streams LSL do pipeline existente")]
    public string decoderStreamName = "Signal";
    public string decoderStreamType = "BCI";
    public string markerStreamName = "GrazMI_Markers";
    public string markerStreamType = "Markers";

    [Header("Controle motor LSL")]
    [Tooltip("Stream contínuo produzido pelo controlador Python. Os dois primeiros canais são left_leg e right_leg.")]
    public string controlStreamName = "GrazMI_Control";
    public string controlStreamType = "BCIControl";

    [Header("Compatibilidade legada do Signal/BCI")]
    [Tooltip("Legado. Mantido para cenas antigas; Signal/BCI não controla mais as pernas.")]
    [Range(0.5f, 1f)] public float decoderThreshold = 0.60f;

    [Tooltip("Legado. Mantido apenas para compatibilidade de serialização.")]
    public bool continuousDecoderAmplitude = false;

    [Tooltip("Legado. BOTH não faz parte do protocolo experimental atual.")]
    public bool allowBothClass = true;

    [Header("Bola")]
    [Tooltip("Tempo entre o início de ATTEMPT e a chegada da bola ao alvo.")]
    [Min(0.1f)] public float ballArrivalTime = 2.5f;
    public float ballSpeed = 3f;
    public Vector2 trajectoryMin = new Vector2(-25f, 5f);
    public Vector2 trajectoryMax = new Vector2(25f, 25f);
    [Min(1)] public int trajectoryBufferSize = 256;

    [Header("Feedback opcional")]
    public bool sendUnityFeedback = false;
    public string feedbackStreamName = "Unity_Game1_Feedback";
    public string feedbackStreamType = "Markers";

    private string dataRootFolder;
    private string representationManifestPath = string.Empty;
    private bool sessionActive;
    private bool attemptActive;
    private bool cameraAdjusting;
    private bool cameraHasBeenAdjusted;
    private bool originalCameraFovSaved;
    private float originalCameraFov;
    private int currentTrial = 1;

    private bool hasCue;
    private bool hasSeenTrialCue;
    private bool unityLogActive;
    private int cueCode;
    private BallDirection cuedDirection;

    private string decoderLslStatus = "aguardando Signal";
    private string controlLslStatus = "aguardando GrazMI_Control";

    private int hitCount;
    private int missCount;
    private bool legacyBallFeedbackStateSaved;
    private bool previousLegacyBallFeedback;

    private PlayerController playerController;
    private GameUIUm gameUI;
    private StartUm startGame;
    private PsychoPyMarkerReceiver markerReceiver;
    private BCIDecoderReceiver decoderReceiver;
    private BCIControlReceiver controlReceiver;
    private Game1OutcomeOutlet outcomeOutlet;
    private RealtimeLog realtimeLog;
    private BallLauncher launcher;

    private void Awake()
    {
        markerReceiver = GetComponent<PsychoPyMarkerReceiver>();
        if (markerReceiver == null)
            markerReceiver = gameObject.AddComponent<PsychoPyMarkerReceiver>();

        decoderReceiver = GetComponent<BCIDecoderReceiver>();
        if (decoderReceiver == null)
            decoderReceiver = gameObject.AddComponent<BCIDecoderReceiver>();

        controlReceiver = GetComponent<BCIControlReceiver>();
        if (controlReceiver == null)
            controlReceiver = gameObject.AddComponent<BCIControlReceiver>();

        outcomeOutlet = GetComponent<Game1OutcomeOutlet>();
        if (outcomeOutlet == null)
            outcomeOutlet = gameObject.AddComponent<Game1OutcomeOutlet>();

        markerReceiver.MarkerReceived += OnMarkerReceived;
        markerReceiver.StatusChanged += OnMarkerStatusChanged;

        decoderReceiver.SampleReceived += OnDecoderSample;
        decoderReceiver.StatusChanged += OnDecoderStatusChanged;

        controlReceiver.SampleReceived += OnControlSample;
        controlReceiver.StatusChanged += OnControlStatusChanged;

        chooseFolderButton?.onClick.AddListener(ChooseOutputFolder);
        onlineBlockButton?.onClick.AddListener(ToggleOnlineBlock);
        participantInput?.onValueChanged.AddListener(_ => RefreshOutputPath());
        sessionInput?.onValueChanged.AddListener(_ => RefreshOutputPath());
        feedbackModeButton?.onClick.AddListener(ToggleFeedbackMode);
        chooseRepresentationMapButton?.onClick.AddListener(ChooseRepresentationMap);
        cameraAdjustButton?.onClick.AddListener(BeginCameraAdjustment);
        cameraFovSlider?.onValueChanged.AddListener(OnFovSliderChanged);
        cameraDoneButton?.onClick.AddListener(FinishCameraAdjustment);
        startButton?.onClick.AddListener(StartExperiment);
        backButton?.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        if (participantInput != null && string.IsNullOrWhiteSpace(participantInput.text))
            participantInput.text = "LR102";

        if (sessionInput != null && string.IsNullOrWhiteSpace(sessionInput.text))
            sessionInput.text = "1";

        // A interface seleciona apenas a RAIZ dos dados. As pastas internas
        // <subject>/S<session>/online são resolvidas automaticamente.
        dataRootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "Dados");

        ApplyModeFromOnlineBlock();
        UpdateOnlineBlockUI();
        RefreshOutputPath();
        SetMarkerStatus("aguardando GrazMI_Markers");
        decoderLslStatus = "aguardando Signal";
        controlLslStatus = "aguardando GrazMI_Control";
        RefreshBCIStatus();

        UpdateFeedbackModeUI();
        RefreshRepresentationPath();
        representationView?.SetVisible(false);
        classifierFeedbackView?.ResetView();
        classifierFeedbackView?.SetVisible(false);

        // Cria/recupera os overlays em runtime também, caso as referências
        // serializadas da cena tenham sido perdidas.
        EnsureCueCommandUI();
        EnsureScoreUI();
        HideCueCommand();
        SetScoreVisible(false);
        UpdateScoreUI();

        cameraAdjustRoot?.SetActive(false);
        StartCoroutine(ApplyDefaultCameraWhenReady());
    }

    private IEnumerator ApplyDefaultCameraWhenReady()
    {
        for (int i = 0; i < 180; i++)
        {
            gameUI = GameUIUm.Instance;
            if (gameUI == null)
                gameUI = FindAnyObjectByType<GameUIUm>();

            playerController = PlayerController.instance;

            if (gameUI != null && playerController != null && playerController.mainCam != null)
                break;

            yield return null;
        }

        ApplyDefaultCameraViewIfNeeded();
    }

    private void ApplyDefaultCameraViewIfNeeded()
    {
        if (!applyDefaultCameraView || cameraHasBeenAdjusted)
            return;

        if (gameUI == null)
        {
            gameUI = GameUIUm.Instance;
            if (gameUI == null)
                gameUI = FindAnyObjectByType<GameUIUm>();
        }

        if (playerController == null)
            playerController = PlayerController.instance;

        if (playerController == null || playerController.mainCam == null)
            return;

        Camera cam = playerController.mainCam;
        cam.gameObject.SetActive(true);
        SaveOriginalCameraFovIfNeeded(cam);

        if (playerController.lateralCam != null)
            playerController.lateralCam.gameObject.SetActive(false);

        cam.transform.localRotation = Quaternion.Euler(defaultCameraRotation, 0f, 0f);
        cam.fieldOfView = defaultCameraFov;

        Vector3 pos = cam.transform.localPosition;
        pos.z = Mathf.Lerp(mainCameraZMin, mainCameraZMax, defaultCameraHorizontal);
        pos.y = Mathf.Lerp(mainCameraYMin, mainCameraYMax, defaultCameraVertical);
        cam.transform.localPosition = pos;

        // Mantém os sliders coerentes com a visão padrão, sem disparar listeners.
        if (gameUI != null && gameUI.statusUI != null)
        {
            StatusUIController status = gameUI.statusUI;

            if (status.camRotation != null)
                status.camRotation.SetValueWithoutNotify(defaultCameraRotation);

            if (status.camHorizontalMove != null)
                status.camHorizontalMove.SetValueWithoutNotify(defaultCameraHorizontal);

            if (status.camVerticalMove != null)
                status.camVerticalMove.SetValueWithoutNotify(defaultCameraVertical);
        }

        ConfigureAndSyncFovSlider(cam);
    }

    private void SaveOriginalCameraFovIfNeeded(Camera cam)
    {
        if (cam == null || originalCameraFovSaved)
            return;

        originalCameraFov = cam.fieldOfView;
        originalCameraFovSaved = true;
    }

    private void ConfigureAndSyncFovSlider(Camera cam)
    {
        if (cameraFovSlider == null || cam == null)
            return;

        cameraFovSlider.minValue = minCameraFov;
        cameraFovSlider.maxValue = maxCameraFov;
        cameraFovSlider.wholeNumbers = false;
        cameraFovSlider.SetValueWithoutNotify(cam.fieldOfView);
    }

    private void OnFovSliderChanged(float value)
    {
        if (playerController == null)
            playerController = PlayerController.instance;

        if (playerController == null || playerController.mainCam == null)
            return;

        playerController.mainCam.fieldOfView = Mathf.Clamp(value, minCameraFov, maxCameraFov);
        UpdateCameraValues();
    }

    private void Update()
    {
        // O status BCI é atualizado pelos eventos do BCIDecoderReceiver.
        if (cameraAdjusting)
        {
            UpdateCameraValues();
            return;
        }

        if (!sessionActive || gameUI == null)
            return;

        if (gameUI.spaceBar != null && gameUI.spaceBar.activeSelf)
            gameUI.spaceBar.SetActive(false);

        if (gameUI.configsUI != null && gameUI.configsUI.activeSelf)
            gameUI.configsUI.SetActive(false);
    }

    public void BeginCameraAdjustment()
    {
        if (sessionActive)
            return;

        gameUI = GameUIUm.Instance;
        if (gameUI == null)
            gameUI = FindAnyObjectByType<GameUIUm>();

        playerController = PlayerController.instance;

        if (gameUI == null || playerController == null || gameUI.configsUI == null)
        {
            SetMarkerStatus("ajuste de câmera indisponível");
            return;
        }

        // Mantém a câmera principal em POV.
        // O ajuste usa rotação X, posição local Y/Z e FOV.
        if (playerController.mainCam != null)
        {
            playerController.mainCam.gameObject.SetActive(true);
            SaveOriginalCameraFovIfNeeded(playerController.mainCam);
        }

        if (playerController.lateralCam != null)
            playerController.lateralCam.gameObject.SetActive(false);

        ConfigureCameraAdjustmentPanel(gameUI.configsUI);

        // Usa os valores padrão específicos do Game 1 na primeira abertura.
        // Depois de um ajuste manual, mantém exatamente a visão escolhida.
        ApplyDefaultCameraViewIfNeeded();

        if (playerController.mainCam != null)
            ConfigureAndSyncFovSlider(playerController.mainCam);

        menuRoot?.SetActive(false);
        gameUI.configsUI.SetActive(true);
        cameraAdjustRoot?.SetActive(true);

        cameraAdjusting = true;
        UpdateCameraValues();
    }

    public void FinishCameraAdjustment()
    {
        if (!cameraAdjusting)
            return;

        UpdateCameraValues();
        Debug.Log($"[Game 1] Câmera ajustada: {CurrentCameraValuesOneLine()}");

        cameraHasBeenAdjusted = true;
        cameraAdjusting = false;

        if (gameUI != null && gameUI.configsUI != null)
            gameUI.configsUI.SetActive(false);

        cameraAdjustRoot?.SetActive(false);
        menuRoot?.SetActive(true);
        SetMarkerStatus("câmera ajustada");
    }

    private static void ConfigureCameraAdjustmentPanel(GameObject configs)
    {
        if (configs == null)
            return;

        SetChildActive(configs.transform, "Rotacao", true);
        SetChildActive(configs.transform, "Position Horizontal", true);
        SetChildActive(configs.transform, "Position Vertical", true);

        // O FOV é controlado pelo slider próprio no HUD superior.
        // O "Zoom" antigo continua oculto para não interferir na câmera principal.
        SetChildActive(configs.transform, "Zoom", false);
        SetChildActive(configs.transform, "Input Esquerdo", false);
        SetChildActive(configs.transform, "Input Direito", false);
        SetChildActive(configs.transform, "Buttons", false);
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private void UpdateCameraValues()
    {
        if (cameraValuesText == null)
            return;

        if (gameUI == null || gameUI.statusUI == null || playerController == null || playerController.mainCam == null)
        {
            cameraValuesText.text = "Câmera indisponível";
            return;
        }

        StatusUIController status = gameUI.statusUI;
        Vector3 pos = playerController.mainCam.transform.localPosition;
        float rotX = NormalizeSignedAngle(playerController.mainCam.transform.localEulerAngles.x);

        float sliderRot = status.camRotation != null ? status.camRotation.value : rotX;
        float sliderHorizontal = status.camHorizontalMove != null ? status.camHorizontalMove.value : 0f;
        float sliderVertical = status.camVerticalMove != null ? status.camVerticalMove.value : 0f;

        float fov = playerController.mainCam.fieldOfView;

        cameraValuesText.text =
            $"Rot {sliderRot:F1}° | H {sliderHorizontal:F3} | V {sliderVertical:F3} | FOV {fov:F1}°";
    }

    private string CurrentCameraValuesOneLine()
    {
        if (gameUI == null || gameUI.statusUI == null || playerController == null || playerController.mainCam == null)
            return "indisponível";

        StatusUIController status = gameUI.statusUI;
        Vector3 pos = playerController.mainCam.transform.localPosition;

        float sliderRot = status.camRotation != null
            ? status.camRotation.value
            : NormalizeSignedAngle(playerController.mainCam.transform.localEulerAngles.x);

        float sliderHorizontal = status.camHorizontalMove != null
            ? status.camHorizontalMove.value
            : 0f;

        float sliderVertical = status.camVerticalMove != null
            ? status.camVerticalMove.value
            : 0f;

        float fov = playerController.mainCam.fieldOfView;
        return $"Rot={sliderRot:F3}; Horizontal={sliderHorizontal:F4}; Vertical={sliderVertical:F4}; FOV={fov:F2}; Y={pos.y:F3}; Z={pos.z:F3}";
    }

    private static float NormalizeSignedAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    public void ChooseOutputFolder()
    {
        StartCoroutine(ChooseOutputFolderCoroutine());
    }

    private IEnumerator ChooseOutputFolderCoroutine()
    {
        FileBrowser.SetFilters(false);

        string initialFolder = Directory.Exists(dataRootFolder)
            ? dataRootFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Folders,
            false,
            initialFolder,
            null,
            "Selecione a pasta RAIZ dos dados (ex.: Dados)",
            "Selecionar");

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
            yield break;

        dataRootFolder = FileBrowser.Result[0];
        RefreshOutputPath();
    }

    public void ToggleOnlineBlock()
    {
        if (sessionActive)
            return;

        // Ordem operacional: TREINO -> online PCA -> online sem feedback -> TREINO.
        switch (onlineBlock)
        {
            case Game1OnlineBlock.Treino:
                onlineBlock = Game1OnlineBlock.IM_online_PCA;
                break;
            case Game1OnlineBlock.IM_online_PCA:
                onlineBlock = Game1OnlineBlock.IM_online_sem_feedback;
                break;
            default:
                onlineBlock = Game1OnlineBlock.Treino;
                break;
        }

        ApplyModeFromOnlineBlock();
        UpdateOnlineBlockUI();
        UpdateFeedbackModeUI();
        RefreshOutputPath();
    }

    private void UpdateOnlineBlockUI()
    {
        if (onlineBlockButtonText != null)
            onlineBlockButtonText.text = $"BLOCO: {OnlineBlockDisplayName()}";
    }

    private string OnlineBlockDisplayName()
    {
        switch (onlineBlock)
        {
            case Game1OnlineBlock.Treino:
                return "TREINO (EM/IM)";
            case Game1OnlineBlock.IM_online_PCA:
                return "ONLINE COM FEEDBACK";
            default:
                return "ONLINE SEM FEEDBACK";
        }
    }

    // Nome persistido nos arquivos continua igual para não quebrar a organização
    // de dados/Python já existente.
    private string OnlineBlockName()
    {
        switch (onlineBlock)
        {
            case Game1OnlineBlock.Treino:
                return "TREINO (EM/IM)";
            case Game1OnlineBlock.IM_online_PCA:
                return "IM_online_PCA";
            default:
                return "IM_online_sem_feedback";
        }
    }

    private bool IsTrainingBlock()
    {
        return onlineBlock == Game1OnlineBlock.Treino;
    }

    private bool ProducesUnityLog()
    {
        return !IsTrainingBlock();
    }

    private void ApplyModeFromOnlineBlock()
    {
        switch (onlineBlock)
        {
            case Game1OnlineBlock.Treino:
                feedbackMode = Game1FeedbackMode.AvatarCueTraining;
                break;
            case Game1OnlineBlock.IM_online_PCA:
                feedbackMode = Game1FeedbackMode.AvatarRepresentationCue;
                break;
            default:
                feedbackMode = Game1FeedbackMode.AvatarAndBalls;
                break;
        }
    }

    public void ToggleFeedbackMode()
    {
        // Compatibilidade com cenas antigas: qualquer botão de modo residual
        // passa a fazer a mesma coisa que o botão de bloco.
        ToggleOnlineBlock();
    }

    private bool UsesClassifierFeedback()
    {
        return feedbackMode == Game1FeedbackMode.AvatarRepresentationCue;
    }

    private bool UsesRepresentation()
    {
        // Mantido apenas para compatibilidade com campos/código antigos.
        // O PCA continua chegando em Signal/BCI, mas não é desenhado no Unity.
        return false;
    }

    private bool UsesBalls()
    {
        return feedbackMode == Game1FeedbackMode.AvatarAndBalls ||
               feedbackMode == Game1FeedbackMode.AvatarRepresentationAndBalls;
    }

    private bool UsesScreenCue()
    {
        return feedbackMode == Game1FeedbackMode.AvatarRepresentationCue ||
               feedbackMode == Game1FeedbackMode.AvatarCueTraining;
    }

    private void UpdateFeedbackModeUI()
    {
        if (feedbackModeButtonText != null)
        {
            switch (feedbackMode)
            {
                case Game1FeedbackMode.AvatarRepresentationAndBalls:
                    feedbackModeButtonText.text = "MODO: AVATAR + MAPA + BOLAS";
                    break;

                case Game1FeedbackMode.AvatarRepresentationCue:
                    feedbackModeButtonText.text = "MODO: AVATAR + CLASSIFICADOR + INDICADOR";
                    break;

                case Game1FeedbackMode.AvatarCueTraining:
                    feedbackModeButtonText.text = "MODO: TREINO GUIADO";
                    break;

                default:
                    feedbackModeButtonText.text = "MODO: AVATAR + BOLAS";
                    break;
            }
        }

        if (chooseRepresentationMapButton != null)
            chooseRepresentationMapButton.interactable = false;

        ApplyFeedbackLayout();

        if (!UsesScreenCue())
            HideCueCommand();

        SetScoreVisible(sessionActive && UsesBalls());
    }

    private void ApplyFeedbackLayout()
    {
        if (classifierFeedbackView != null && classifierFeedbackView.root != null)
        {
            RectTransform classifierRect = classifierFeedbackView.root.GetComponent<RectTransform>();
            if (classifierRect != null)
            {
                classifierRect.anchorMin = classifierRect.anchorMax = new Vector2(0.5f, 1f);
                classifierRect.pivot = new Vector2(0.5f, 1f);
                classifierRect.anchoredPosition = classifierFeedbackPosition;
                classifierRect.sizeDelta = classifierFeedbackSize;
            }
        }

        if (representationView != null && representationView.root != null)
        {
            RectTransform rect = representationView.root.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Mesmo tamanho e aspecto nos dois modos com representação.
                // A posição pode mudar, mas a visualização não muda de proporção.
                rect.sizeDelta = representationMapSize;

                if (feedbackMode == Game1FeedbackMode.AvatarRepresentationCue)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = representationMapPositionWithCommands;
                }
                else
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    rect.anchoredPosition = representationMapPositionWithBalls;
                }
            }
        }

        if (cueCommandRoot != null)
        {
            RectTransform cueRect = cueCommandRoot.GetComponent<RectTransform>();
            if (cueRect != null)
            {
                cueRect.anchorMin = Vector2.zero;
                cueRect.anchorMax = Vector2.one;
                cueRect.pivot = new Vector2(0.5f, 0.5f);
                cueRect.anchoredPosition = Vector2.zero;
                cueRect.sizeDelta = Vector2.zero;
                cueRect.offsetMin = Vector2.zero;
                cueRect.offsetMax = Vector2.zero;
            }
        }
    }

    public void ChooseRepresentationMap()
    {
        if (sessionActive)
            return;

        StartCoroutine(ChooseRepresentationMapCoroutine());
    }

    private IEnumerator ChooseRepresentationMapCoroutine()
    {
        FileBrowser.SetFilters(false);

        string initialPath = string.IsNullOrWhiteSpace(representationManifestPath)
            ? dataRootFolder
            : (Path.GetDirectoryName(representationManifestPath) ?? dataRootFolder);

        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            initialPath,
            null,
            "Selecione *_representation_map.json",
            "Selecionar");

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
            yield break;

        string selected = FileBrowser.Result[0];
        if (!selected.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            SetMarkerStatus("mapa inválido: selecione o manifesto .json");
            yield break;
        }

        if (representationView == null)
        {
            SetMarkerStatus("RepresentationFeedbackView não encontrado");
            yield break;
        }

        if (!representationView.LoadManifest(selected, out string error))
        {
            SetMarkerStatus($"falha ao carregar mapa: {error}");
            yield break;
        }

        representationManifestPath = selected;
        RefreshRepresentationPath();
        SetMarkerStatus(
            $"mapa carregado: {representationView.RepresentationName} ({representationView.Dimensions}D)");
    }

    private void RefreshRepresentationPath()
    {
        if (representationMapPathText == null)
            return;

        if (string.IsNullOrWhiteSpace(representationManifestPath))
        {
            representationMapPathText.text = "Mapa: não selecionado";
            return;
        }

        representationMapPathText.text = $"Mapa: {Path.GetFileName(representationManifestPath)}";
    }


    public void StartExperiment()
    {
        if (cameraAdjusting)
            FinishCameraAdjustment();

        string participant = NormalizeSubjectId(participantInput != null ? participantInput.text : null);
        if (string.IsNullOrWhiteSpace(participant))
        {
            SetMarkerStatus("informe o Subject ID");
            return;
        }

        string sessionNumber = NormalizeSessionNumber(sessionInput != null ? sessionInput.text : null);
        if (string.IsNullOrWhiteSpace(sessionNumber))
        {
            SetMarkerStatus("informe uma sessão válida (ex.: 1 ou S1)");
            return;
        }

        gameUI = GameUIUm.Instance;
        playerController = PlayerController.instance;
        ApplyDefaultCameraViewIfNeeded();

        startGame = gameUI != null ? gameUI.startUm : FindAnyObjectByType<StartUm>();
        realtimeLog = RealtimeLog.instance;
        launcher = startGame != null ? startGame.shootBall : BallLauncher.instance;

        bool ballMode = UsesBalls();
        bool classifierFeedbackMode = UsesClassifierFeedback();
        bool trainingMode = IsTrainingBlock();
        bool logMode = ProducesUnityLog();

        if (gameUI == null || startGame == null ||
            (logMode && realtimeLog == null) || playerController == null ||
            (ballMode && launcher == null))
        {
            SetMarkerStatus("componentes do Game 1 não encontrados");
            return;
        }

        if (classifierFeedbackMode && classifierFeedbackView == null)
        {
            SetMarkerStatus("ClassifierProbabilityView não encontrado");
            return;
        }

        string logPath = string.Empty;
        if (logMode)
        {
            if (!TryBuildLogPath(participant, sessionNumber, out logPath, out string pathError))
            {
                SetMarkerStatus(pathError);
                return;
            }

            ResetLog();
            gameUI._pathLog = logPath;
        }
        unityLogActive = false;

        if (ballMode && launcher != null)
        {
            Vector2[] trajectories = new Vector2[Mathf.Max(1, trajectoryBufferSize)];
            for (int i = 0; i < trajectories.Length; i++)
            {
                trajectories[i] = new Vector2(
                    UnityEngine.Random.Range(trajectoryMin.x, trajectoryMax.x),
                    UnityEngine.Random.Range(trajectoryMin.y, trajectoryMax.y));
            }

            launcher.SetParameters(ballSpeed, trajectories);
            launcher.BallResolved -= OnBallResolved;
            launcher.BallResolved += OnBallResolved;
        }
        else if (launcher != null && launcher.HasActiveBall)
        {
            launcher.CancelActiveBall();
        }

        startGame.actTrials = 0;
        startGame.Started = true;
        startGame.isInitialize = true;

        sessionActive = true;
        attemptActive = false;
        currentTrial = 1;
        hasCue = false;
        hasSeenTrialCue = false;
        cueCode = 0;

        ResetScore();
        SetScoreVisible(ballMode);

        // O Game 1 usa o placar em vez dos pop-ups legados.
        // O estado anterior é restaurado ao terminar, preservando o Game 2.
        if (ballMode && launcher != null)
        {
            previousLegacyBallFeedback = launcher.ShowLegacyOutcomeFeedback;
            legacyBallFeedbackStateSaved = true;
            launcher.ShowLegacyOutcomeFeedback = false;
        }

        SetLegInputs(0f, 0f, 0f);
        HideCueCommand();

        if (representationView != null)
            representationView.SetVisible(false);

        if (classifierFeedbackView != null)
        {
            classifierFeedbackView.ResetView();
            classifierFeedbackView.SetVisible(classifierFeedbackMode);
        }

        if (gameUI.spaceBar != null)
            gameUI.spaceBar.SetActive(false);

        if (logMode)
        {
            gameUI.StartLogRealtime();
            unityLogActive = true;
        }

        if (!trainingMode)
        {
            decoderReceiver.Configure(decoderStreamName, decoderStreamType);
            decoderReceiver.StartReceiving();
            decoderLslStatus = $"procurando {decoderStreamName}";

            controlReceiver.Configure(controlStreamName, controlStreamType);
            controlReceiver.StartReceiving();
            controlLslStatus = $"procurando {controlStreamName}";
        }
        else
        {
            // TREINO é inteiramente dirigido pelos marcadores do PsychoPy.
            // O input BCI não deve interferir na demonstração do movimento.
            decoderReceiver.StopReceiving();
            controlReceiver.StopReceiving();
            decoderLslStatus = "treino: não utilizado";
            controlLslStatus = "treino: ignorado";
        }
        RefreshBCIStatus();

        markerReceiver.Configure(markerStreamName, markerStreamType);
        markerReceiver.StartReceiving();
        SetMarkerStatus($"procurando {markerStreamName}");

        if (sendUnityFeedback)
        {
            outcomeOutlet.Configure(feedbackStreamName, feedbackStreamType);
            outcomeOutlet.Send(101);
        }

        if (gameUI.configsUI != null)
            gameUI.configsUI.SetActive(false);

        menuRoot?.SetActive(false);
        Debug.Log(trainingMode
            ? "Game 1/TREINO iniciado. Sem log Unity; movimento guiado pelos marcadores."
            : $"Game 1/BCI iniciado. Log Unity: {logPath}");
    }

    private void OnMarkerReceived(int code, double timestamp)
    {
        if (!sessionActive || startGame == null)
            return;

        if (unityLogActive && realtimeLog != null)
        {
            realtimeLog._marcadorNum = code;
            realtimeLog._marcador = MarkerLabel(code);
        }

        switch (code)
        {
            case 1: // BASELINE
                attemptActive = false;
                hasCue = false;
                if (IsTrainingBlock())
                    SetLegInputs(0f, 0f, 0f);
                representationView?.SetAttemptActive(false);
                representationView?.ClearTrail();
                HideCueCommand();

                SetMarkerStatus("BASELINE");
                break;

            case 2: // ATTENTION
                attemptActive = false;
                if (IsTrainingBlock())
                    SetLegInputs(0f, 0f, 0f);
                representationView?.SetAttemptActive(false);
                representationView?.ClearTrail();
                HideCueCommand();

                SetMarkerStatus($"ATTENTION — trial {currentTrial}");
                break;

            case 3: // LEFT_MI_STIM
                StoreCue(BallDirection.Left, code);
                break;

            case 4: // RIGHT_MI_STIM
                StoreCue(BallDirection.Right, code);
                break;

            case 6: // REST (fase entre tentativas)
                attemptActive = false;
                representationView?.SetAttemptActive(false);

                // No TREINO, encerra a demonstração e devolve as pernas ao repouso.
                // Nos blocos online, NÃO interfere no GrazMI_Control contínuo.
                if (IsTrainingBlock())
                    SetLegInputs(0f, 0f, 0f);

                HideCueCommand();
                SetMarkerStatus($"REST — trial {currentTrial}");
                break;

            case 8: // REST_STIM
                // REST_STIM é a CONDIÇÃO experimental de repouso e usa o alvo central.
                // Não há qualquer comando bilateral associado a este marcador.
                StoreCue(BallDirection.Center, code);
                break;

            case 7: // BOTH_LEGS / BOTH_MI_STIM legado
                // Compatibilidade de arquivos/protocolos antigos: mantém a trajetória central.
                // O movimento das pernas continua vindo exclusivamente de GrazMI_Control.
                StoreCue(BallDirection.Center, code);
                break;

            case 5: // ATTEMPT
                BeginAttempt();
                break;

            case 99: // BLOCK_END
                HideCueCommand();
                FinishExperiment();
                break;

            default:
                Debug.LogWarning($"Marcador GrazMI não reconhecido: {code}");
                break;
        }

        Debug.Log($"GrazMI marker {code} ({MarkerLabel(code)}) at {timestamp:F6}");
    }

    private void StoreCue(BallDirection direction, int code)
    {
        // REST_STIM é condição experimental, não marcador de encerramento.
        // A tentativa avança quando chega o próximo cue.
        if (hasSeenTrialCue)
            currentTrial++;
        else
            hasSeenTrialCue = true;

        attemptActive = false;

        // Somente o TREINO é dirigido pelos marcadores. Nos blocos online,
        // o GrazMI_Control pode mover as pernas continuamente, inclusive fora
        // de ATTEMPT, portanto o cue jamais zera/força a saída BCI.
        if (IsTrainingBlock())
            SetLegInputs(0f, 0f, 0f);

        representationView?.SetAttemptActive(false);
        representationView?.ClearTrail();

        hasCue = true;
        cueCode = code;
        cuedDirection = direction;

        if (unityLogActive && realtimeLog != null)
        {
            realtimeLog._classe = code;
            realtimeLog._tentativa = currentTrial;
            realtimeLog._target = DirectionLabel(direction);
            realtimeLog._acerto = "NEUTRO";
        }

        if (UsesScreenCue())
            ShowSpatialCue(code, false);
        else
            HideCueCommand();

        SetMarkerStatus($"{MarkerLabel(code)} — planejamento — trial {currentTrial}");
    }

    private void BeginAttempt()
    {
        if (!hasCue)
        {
            SetMarkerStatus($"ATTEMPT sem cue — trial {currentTrial}");
            Debug.LogWarning("ATTEMPT recebido antes de LEFT_MI_STIM/RIGHT_MI_STIM/REST_STIM.");
            return;
        }

        attemptActive = true;
        representationView?.SetAttemptActive(UsesRepresentation());

        if (UsesScreenCue())
            ShowSpatialCue(cueCode, true);

        if (IsTrainingBlock())
            ApplyTrainingMovement();

        if (unityLogActive && realtimeLog != null)
        {
            realtimeLog._tentativa = currentTrial;
            realtimeLog._classe = cueCode;
            realtimeLog._target = DirectionLabel(cuedDirection);
            realtimeLog._acerto = "NEUTRO";
        }
        startGame.actTrials = currentTrial - 1;

        if (UsesBalls())
        {
            // Garante sincronização com o PsychoPy mesmo que uma bola antiga
            // não tenha resolvido.
            if (launcher != null && launcher.HasActiveBall)
                launcher.CancelActiveBall();

            if (launcher == null)
            {
                SetMarkerStatus("ATTEMPT — BallLauncher não encontrado");
                return;
            }

            GameObject ball = launcher.LaunchBall(cuedDirection, ballArrivalTime);
            if (ball == null)
                SetMarkerStatus($"ATTEMPT — falha ao lançar bola ({DirectionLabel(cuedDirection)})");
            else
                SetMarkerStatus(
                    $"ATTEMPT — {DirectionLabel(cuedDirection)} — trial {currentTrial}" +
                    (UsesClassifierFeedback() ? " — feedback do classificador ativo" : string.Empty));
        }
        else
        {
            // Variante sem bolas: a instrução vem diretamente do marcador PsychoPy
            // e as pernas continuam sendo o feedback motor contínuo.
            if (launcher != null && launcher.HasActiveBall)
                launcher.CancelActiveBall();

            SetMarkerStatus(
                $"ATTEMPT — comando {DirectionLabel(cuedDirection)} — trial {currentTrial}");
        }
    }

    private void ApplyTrainingMovement()
    {
        // TREINO: o avatar demonstra a resposta correta. O input BCI é ignorado.
        switch (cueCode)
        {
            case 3: // LEFT_MI_STIM
                SetLegInputs(0f, 0f, 1f);
                break;
            case 4: // RIGHT_MI_STIM
                SetLegInputs(1f, 0f, 0f);
                break;
            case 7: // BOTH_LEGS legado/compatibilidade
                SetLegInputs(0f, 1f, 0f);
                break;
            case 8: // REST_STIM
            default:
                SetLegInputs(0f, 0f, 0f);
                break;
        }
    }

    private void EndAttemptLegacy()
    {
        // Mantido somente para facilitar compatibilidade/debug de cenas antigas.
        // No protocolo atual, REST_STIM é uma condição e NÃO chama este método.
        attemptActive = false;
        SetLegInputs(0f, 0f, 0f);
        representationView?.SetAttemptActive(false);
        HideCueCommand();

        if (launcher != null && launcher.HasActiveBall)
            launcher.CancelActiveBall();
    }

    private void OnDecoderSample(
        float rep1,
        float rep2,
        float pLeft,
        float pRest,
        float pRight,
        double timestamp)
    {
        if (!sessionActive)
            return;

        // Signal/BCI permanece independente do comando motor.
        // rep1/rep2 seguem disponíveis para dashboard/log, enquanto o Unity
        // mostra predict_proba LEFT/REST/RIGHT no bloco com feedback.
        if (UsesClassifierFeedback() && classifierFeedbackView != null)
        {
            classifierFeedbackView.PushProbabilities(
                pLeft,
                pRest,
                pRight,
                decoderReceiver.ActiveLeft,
                decoderReceiver.ActiveRest,
                decoderReceiver.ActiveRight,
                decoderReceiver.LastPredictedClassId,
                decoderReceiver.LastConfidence);
        }

        if (realtimeLog != null)
        {
            realtimeLog._rep1 = rep1;
            realtimeLog._rep2 = rep2;
            realtimeLog._decoderLeft = pLeft;
            realtimeLog._decoderRest = pRest;
            realtimeLog._decoderRight = pRight;
        }
    }

    private void OnControlSample(
        float leftLeg,
        float rightLeg,
        float rest,
        float left,
        float both,
        float right,
        float confidence,
        float densityGate,
        int stateId,
        double timestamp)
    {
        if (!sessionActive)
            return;

        if (classifierFeedbackView != null)
            classifierFeedbackView.SetControlState(stateId, confidence);

        // TREINO é dirigido exclusivamente pelos marcadores do PsychoPy.
        if (IsTrainingBlock())
            return;

        // Nos blocos online, GrazMI_Control controla as pernas CONTINUAMENTE.
        // Não há gate por ATTEMPT e os marcadores não sobrescrevem o controle.
        // Cue experimental e saída BCI continuam independentes.
        SetLegInputs(
            Mathf.Clamp01(rightLeg),
            0f,
            Mathf.Clamp01(leftLeg));
    }

    private void SetLegInputs(float right, float both, float left)
    {
        if (playerController == null)
            playerController = PlayerController.instance;

        if (playerController != null)
            playerController.UpdateLegValues(right, both, left);
    }

    private void OnBallResolved(BallDirection direction, bool hit)
    {
        if (!sessionActive)
            return;

        if (unityLogActive && realtimeLog != null)
            realtimeLog._acerto = hit ? "ACERTOU" : "ERROU";

        if (hit)
            hitCount++;
        else
            missCount++;

        UpdateScoreUI();

        if (sendUnityFeedback)
            outcomeOutlet.Send(hit ? 201 : 202);
    }

    private void FinishExperiment()
    {
        if (!sessionActive)
            return;

        sessionActive = false;
        attemptActive = false;
        SetLegInputs(0f, 0f, 0f);
        HideCueCommand();

        if (representationView != null)
        {
            representationView.SetAttemptActive(false);
            representationView.ClearTrail();
            representationView.SetVisible(false);
        }

        classifierFeedbackView?.SetVisible(false);

        markerReceiver?.StopReceiving();
        decoderReceiver?.StopReceiving();
        controlReceiver?.StopReceiving();

        if (sendUnityFeedback)
            outcomeOutlet.Send(299);

        if (launcher != null)
        {
            launcher.BallResolved -= OnBallResolved;
            if (launcher.HasActiveBall)
                launcher.CancelActiveBall();

            if (legacyBallFeedbackStateSaved)
                launcher.ShowLegacyOutcomeFeedback = previousLegacyBallFeedback;
        }

        legacyBallFeedbackStateSaved = false;
        SetScoreVisible(false);

        if (startGame != null)
        {
            startGame.Started = false;
            startGame.isInitialize = false;
            if (startGame.shootBall != null)
                startGame.shootBall.isLaunching = false;
        }

        if (unityLogActive && realtimeLog != null)
        {
            realtimeLog.EndLog();
            unityLogActive = false;
        }

        if (gameUI != null)
        {
            if (gameUI.configsUI != null)
                gameUI.configsUI.SetActive(false);

            if (gameUI.painelConfigs != null)
            {
                gameUI.painelConfigs.alpha = 1f;
                gameUI.painelConfigs.interactable = true;
                gameUI.painelConfigs.blocksRaycasts = true;
            }
        }

        menuRoot?.SetActive(true);
        SetMarkerStatus("BLOCK_END — bloco finalizado");
    }

    public void BackToMainMenu()
    {
        if (cameraAdjusting)
            FinishCameraAdjustment();

        if (sessionActive)
            FinishExperiment();

        LoadScene loader = FindAnyObjectByType<LoadScene>();
        if (loader != null)
            loader.NextScene(0);
    }

    private void ResetLog()
    {
        realtimeLog.logs.Clear();
        realtimeLog.logs.Add(new LogData(
            "TempoEEG",
            "TempoUNITY",
            "Classifier",
            "Rep1",
            "Rep2",
            "DecoderLeft",
            "DecoderRest",
            "DecoderRight",
            "AngleLegR",
            "AngleLegL",
            "xBall",
            "yBall",
            "zBall",
            "Marcadornum",
            "Marcador",
            "Acertos",
            "Classe",
            "Tentativa",
            "Target"));

        realtimeLog._rep1 = 0f;
        realtimeLog._rep2 = 0f;
        realtimeLog._decoderLeft = 0f;
        realtimeLog._decoderRest = 0f;
        realtimeLog._decoderRight = 0f;
        realtimeLog._ballPosition = Vector3.zero;
        realtimeLog._marcadorNum = 0;
        realtimeLog._marcador = string.Empty;
        realtimeLog._classe = 0;
        realtimeLog._tentativa = 1;
        realtimeLog._target = string.Empty;
        realtimeLog._acerto = string.Empty;
    }

    private void ResetScore()
    {
        hitCount = 0;
        missCount = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        EnsureScoreUI();

        if (scoreText != null)
            scoreText.text = $"ACERTOS  {hitCount}   |   ERROS  {missCount}";
    }

    private void SetScoreVisible(bool visible)
    {
        EnsureScoreUI();

        if (scoreRoot != null)
            scoreRoot.SetActive(visible);
    }

    private void EnsureScoreUI()
    {
        if (scoreRoot != null && scoreText != null)
            return;

        Canvas rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
        Transform existing = parent.Find("Game1ScoreOverlay");

        if (existing != null)
        {
            scoreRoot = existing.gameObject;
            scoreText = existing.GetComponentInChildren<TMP_Text>(true);
        }

        if (scoreRoot != null && scoreText != null)
            return;

        GameObject overlay = new GameObject(
            "Game1ScoreOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(Image));

        overlay.layer = LayerMask.NameToLayer("UI");
        overlay.transform.SetParent(parent, false);

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -24f);
        rect.sizeDelta = new Vector2(310f, 58f);

        Canvas overlayCanvas = overlay.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 4900;

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image bg = overlay.GetComponent<Image>();
        bg.color = new Color(0.025f, 0.035f, 0.055f, 0.96f);
        bg.raycastTarget = false;

        GameObject textObject = new GameObject(
            "ScoreText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        textObject.layer = LayerMask.NameToLayer("UI");
        textObject.transform.SetParent(overlay.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "ACERTOS  0   |   ERROS  0";
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        scoreRoot = overlay;
        scoreText = text;
        overlay.SetActive(false);
    }

    private void EnsureCueCommandUI()
    {
        Canvas rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        Transform parent = rootCanvas != null ? rootCanvas.transform : transform;

        if (cueCommandRoot == null)
        {
            Transform existing = parent.Find("Game1CueCommandOverlay");
            if (existing != null)
                cueCommandRoot = existing.gameObject;
        }

        if (cueCommandRoot == null)
        {
            cueCommandRoot = new GameObject(
                "Game1CueCommandOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(Image));
            cueCommandRoot.layer = LayerMask.NameToLayer("UI");
            cueCommandRoot.transform.SetParent(parent, false);
        }

        RectTransform rootRect = cueCommandRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        Canvas overlayCanvas = cueCommandRoot.GetComponent<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = cueCommandRoot.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 5000;

        CanvasGroup group = cueCommandRoot.GetComponent<CanvasGroup>();
        if (group == null)
            group = cueCommandRoot.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image bg = cueCommandRoot.GetComponent<Image>();
        if (bg == null)
            bg = cueCommandRoot.AddComponent<Image>();
        bg.color = Color.clear;
        bg.raycastTarget = false;

        if (cueCommandText == null)
        {
            Transform indicator = cueCommandRoot.transform.Find("CueCommandText");
            if (indicator != null)
                cueCommandText = indicator.GetComponent<TMP_Text>();
        }

        if (cueCommandText == null)
        {
            GameObject indicatorObject = new GameObject(
                "CueCommandText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            indicatorObject.layer = LayerMask.NameToLayer("UI");
            indicatorObject.transform.SetParent(cueCommandRoot.transform, false);
            cueCommandText = indicatorObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform indicatorRect = cueCommandText.GetComponent<RectTransform>();
        indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(180f, 180f);

        cueCommandText.text = "○";
        cueCommandText.font = TMP_Settings.defaultFontAsset;
        cueCommandText.fontSize = cuePlanningFontSize;
        cueCommandText.fontStyle = FontStyles.Bold;
        cueCommandText.alignment = TextAlignmentOptions.Center;
        cueCommandText.color = Color.white;
        cueCommandText.enableWordWrapping = false;
        cueCommandText.raycastTarget = false;

        if (cuePhaseText == null)
        {
            Transform phase = cueCommandRoot.transform.Find("CuePhaseText");
            if (phase != null)
                cuePhaseText = phase.GetComponent<TMP_Text>();
        }

        if (cuePhaseText == null)
        {
            GameObject phaseObject = new GameObject(
                "CuePhaseText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            phaseObject.layer = LayerMask.NameToLayer("UI");
            phaseObject.transform.SetParent(cueCommandRoot.transform, false);
            cuePhaseText = phaseObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform phaseRect = cuePhaseText.GetComponent<RectTransform>();
        phaseRect.anchorMin = phaseRect.anchorMax = new Vector2(0.5f, 0.5f);
        phaseRect.pivot = new Vector2(0.5f, 0.5f);
        phaseRect.sizeDelta = new Vector2(180f, 34f);

        cuePhaseText.text = "PREPARE";
        cuePhaseText.font = TMP_Settings.defaultFontAsset;
        cuePhaseText.fontSize = 22f;
        cuePhaseText.fontStyle = FontStyles.Bold;
        cuePhaseText.alignment = TextAlignmentOptions.Center;
        cuePhaseText.color = Color.white;
        cuePhaseText.enableWordWrapping = false;
        cuePhaseText.raycastTarget = false;

        cueCommandRoot.SetActive(false);
    }

    private Vector2 CuePositionForCode(int code)
    {
        switch (code)
        {
            case 3: return cueLeftPosition;
            case 4: return cueRightPosition;
            case 8: return cueCenterPosition; // REST_STIM exclusivamente
            default: return Vector2.zero;     // não exibido: IsCueCode bloqueia
        }
    }

    private bool IsCueCode(int code)
    {
        // O protocolo visual atual possui somente LEFT, RIGHT e REST_STIM.
        // REST comum (6) é apenas fase entre tentativas e nunca mostra PREPARE.
        // BOTH_LEGS (7) permanece reconhecido no código legado, mas não gera
        // indicador espacial; em particular, nunca usa a posição central.
        return code == 3 || code == 4 || code == 8;
    }

    private void ShowSpatialCue(int code, bool executionPhase)
    {
        if (!UsesScreenCue() || !IsCueCode(code))
        {
            HideCueCommand();
            return;
        }

        EnsureCueCommandUI();
        if (cueCommandRoot == null || cueCommandText == null || cuePhaseText == null)
            return;

        Vector2 position = CuePositionForCode(code);

        RectTransform indicatorRect = cueCommandText.GetComponent<RectTransform>();
        indicatorRect.anchoredPosition = position;

        RectTransform phaseRect = cuePhaseText.GetComponent<RectTransform>();
        phaseRect.anchoredPosition = position + new Vector2(0f, -82f);

        // Planejamento = círculo vazio; execução = círculo cheio.
        // A posição informa a perna; não há texto ESQUERDA/DIREITA.
        cueCommandText.text = executionPhase ? "●" : "○";
        cueCommandText.fontSize = executionPhase ? cueExecutionFontSize : cuePlanningFontSize;
        cuePhaseText.text = executionPhase ? "AGORA" : "PREPARE";

        cueCommandRoot.transform.SetAsLastSibling();
        cueCommandRoot.SetActive(true);
    }

    private void HideCueCommand()
    {
        if (cueCommandRoot != null)
            cueCommandRoot.SetActive(false);
    }

    private void OnMarkerStatusChanged(string status)
    {
        SetMarkerStatus(status);
    }

    private void OnDecoderStatusChanged(string status)
    {
        decoderLslStatus = status;
        RefreshBCIStatus();
    }

    private void OnControlStatusChanged(string status)
    {
        controlLslStatus = status;
        RefreshBCIStatus();
    }

    private void RefreshBCIStatus()
    {
        SetDecoderStatus($"PCA: {decoderLslStatus} | CTRL: {controlLslStatus}");
    }

    private void SetDecoderStatus(string status)
    {
        if (eegStatusText != null)
            eegStatusText.text = status.StartsWith("BCI:") ? status : $"BCI: {status}";
    }

    private void SetMarkerStatus(string status)
    {
        if (markerStatusText != null)
            markerStatusText.text = $"PsychoPy: {status}";
    }

    private void RefreshOutputPath()
    {
        if (outputPathText == null)
            return;

        string subject = NormalizeSubjectId(participantInput != null ? participantInput.text : null);
        string session = NormalizeSessionNumber(sessionInput != null ? sessionInput.text : null);

        if (IsTrainingBlock())
        {
            outputPathText.text =
                "TREINO (EM/IM): sem log Unity\n" +
                "Movimento do avatar guiado pelos marcadores do PsychoPy.";
            return;
        }

        if (string.IsNullOrWhiteSpace(dataRootFolder))
        {
            outputPathText.text = "Raiz de dados: não selecionada";
            return;
        }

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(session))
        {
            outputPathText.text = $"Raiz: {dataRootFolder}\nPreencha Subject ID e sessão para resolver o destino.";
            return;
        }

        string onlineFolder = Path.Combine(dataRootFolder, subject, $"S{session}", onlineFolderName);
        string filenameTemplate = BuildLogFilename(subject, session, "<YYYYMMDD_HHMMSS>");

        string state;
        string subjectFolder = Path.Combine(dataRootFolder, subject);
        string sessionFolder = Path.Combine(subjectFolder, $"S{session}");

        if (!Directory.Exists(dataRootFolder))
            state = "RAIZ NÃO ENCONTRADA";
        else if (!Directory.Exists(subjectFolder))
            state = $"SUBJECT {subject} NÃO ENCONTRADO";
        else if (!Directory.Exists(sessionFolder))
            state = $"SESSÃO S{session} NÃO ENCONTRADA";
        else if (!Directory.Exists(onlineFolder))
            state = "online será criada ao iniciar";
        else
            state = "destino localizado";

        outputPathText.text =
            $"Destino: {onlineFolder}\n" +
            $"Arquivo: {filenameTemplate}\n" +
            $"Status: {state}";
    }

    private bool TryBuildLogPath(string subject, string session, out string logPath, out string error)
    {
        logPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(dataRootFolder) || !Directory.Exists(dataRootFolder))
        {
            error = "pasta raiz de dados não encontrada";
            return false;
        }

        string subjectFolder = Path.Combine(dataRootFolder, subject);
        if (!Directory.Exists(subjectFolder))
        {
            error = $"Subject ID não encontrado em Dados: {subject}";
            return false;
        }

        string sessionFolder = Path.Combine(subjectFolder, $"S{session}");
        if (!Directory.Exists(sessionFolder))
        {
            error = $"sessão S{session} não encontrada para {subject}";
            return false;
        }

        string onlineFolder = Path.Combine(sessionFolder, onlineFolderName);
        try
        {
            if (!Directory.Exists(onlineFolder))
                Directory.CreateDirectory(onlineFolder);
        }
        catch (Exception e)
        {
            error = $"não foi possível criar a pasta online: {e.Message}";
            return false;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = BuildLogFilename(subject, session, stamp);
        logPath = Path.Combine(onlineFolder, filename);

        // O RealtimeLog abre o arquivo com overwrite=false. Evita apagar um
        // arquivo existente caso outra execução tenha produzido o mesmo nome.
        if (File.Exists(logPath))
        {
            error = $"arquivo de log já existe: {filename}";
            logPath = string.Empty;
            return false;
        }

        return true;
    }

    private string BuildLogFilename(string subject, string session, string stamp)
    {
        return $"{subject}_{experimentType}_S{session}_{OnlineBlockName()}_online_log_{stamp}.csv";
    }

    private static string NormalizeSubjectId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string clean = new string(value.Trim().ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());
        return clean;
    }

    private static string NormalizeSessionNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string clean = value.Trim().ToUpperInvariant();
        if (clean.StartsWith("S"))
            clean = clean.Substring(1).Trim();

        if (!int.TryParse(clean, out int session) || session < 1)
            return string.Empty;

        return session.ToString();
    }

    private static string MarkerLabel(int code)
    {
        switch (code)
        {
            case 1: return "BASELINE";
            case 2: return "ATTENTION";
            case 3: return "LEFT_MI_STIM";
            case 4: return "RIGHT_MI_STIM";
            case 7: return "BOTH_LEGS";
            case 5: return "ATTEMPT";
            case 6: return "REST";
            case 8: return "REST_STIM";
            case 99: return "BLOCK_END";
            default: return $"MARKER_{code}";
        }
    }

    private static string DirectionLabel(BallDirection direction)
    {
        switch (direction)
        {
            case BallDirection.Left: return "ESQUERDA";
            case BallDirection.Center: return "CENTRO";
            case BallDirection.Right: return "DIREITA";
            default: return "FORA";
        }
    }

    private static string SanitizeParticipant(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(value.Trim().Where(c => !invalid.Contains(c)).ToArray());
    }

    private void OnDestroy()
    {
        if (legacyBallFeedbackStateSaved && launcher != null)
        {
            launcher.ShowLegacyOutcomeFeedback = previousLegacyBallFeedback;
            legacyBallFeedbackStateSaved = false;
        }

        if (markerReceiver != null)
        {
            markerReceiver.MarkerReceived -= OnMarkerReceived;
            markerReceiver.StatusChanged -= OnMarkerStatusChanged;
        }

        if (decoderReceiver != null)
        {
            decoderReceiver.SampleReceived -= OnDecoderSample;
            decoderReceiver.StatusChanged -= OnDecoderStatusChanged;
        }

        if (controlReceiver != null)
        {
            controlReceiver.SampleReceived -= OnControlSample;
            controlReceiver.StatusChanged -= OnControlStatusChanged;
        }

        if (launcher != null)
            launcher.BallResolved -= OnBallResolved;

        chooseFolderButton?.onClick.RemoveListener(ChooseOutputFolder);
        feedbackModeButton?.onClick.RemoveListener(ToggleFeedbackMode);
        chooseRepresentationMapButton?.onClick.RemoveListener(ChooseRepresentationMap);
        cameraAdjustButton?.onClick.RemoveListener(BeginCameraAdjustment);
        cameraFovSlider?.onValueChanged.RemoveListener(OnFovSliderChanged);
        cameraDoneButton?.onClick.RemoveListener(FinishCameraAdjustment);
        startButton?.onClick.RemoveListener(StartExperiment);
        backButton?.onClick.RemoveListener(BackToMainMenu);

        SetLegInputs(0f, 0f, 0f);
        HideCueCommand();

        // Gameplay é compartilhada com o Game 2.
        if (originalCameraFovSaved)
        {
            if (playerController == null)
                playerController = PlayerController.instance;

            if (playerController != null && playerController.mainCam != null)
                playerController.mainCam.fieldOfView = originalCameraFov;
        }
    }
}
