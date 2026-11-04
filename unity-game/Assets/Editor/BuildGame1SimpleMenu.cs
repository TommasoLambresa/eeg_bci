#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Constrói o menu enxuto diretamente na cena Game UI usando a API do Unity,
/// preservando a interface antiga desativada para permitir restauração.
/// </summary>
public static class BuildGame1SimpleMenu
{
    private const string ScenePath = "Assets/Scenes/Game UI.unity";
    private const string MenuName = "Game1SimpleMenu";
    private const string CameraOverlayName = "Game1CameraAdjustOverlay";
    private const string RepresentationOverlayName = "Game1RepresentationOverlay"; // legado
    private const string ClassifierOverlayName = "Game1ClassifierFeedbackOverlay";
    private const string CueOverlayName = "Game1CueCommandOverlay";
    private const string ScoreOverlayName = "Game1ScoreOverlay";

    [MenuItem("Tools/UNIFESP/Aplicar menu simples do Game 1")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform panel = FindInScene(scene, "Painel de configuracao");
        Transform canvas = FindInScene(scene, "Canvas");

        if (panel == null || canvas == null)
        {
            Debug.LogError("Não foi possível localizar Canvas/Painel de configuracao na cena Game UI.");
            return;
        }

        SetActiveIfFound(panel, "Pages", false);
        SetActiveIfFound(panel, "Tabs", false);
        SetActiveIfFound(panel, "Background", false);
        SetActiveIfFound(panel, "Fechar", false);

        Transform existing = panel.Find(MenuName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        Transform existingCameraOverlay = canvas.Find(CameraOverlayName);
        if (existingCameraOverlay != null)
            Object.DestroyImmediate(existingCameraOverlay.gameObject);

        Transform existingRepresentationOverlay = canvas.Find(RepresentationOverlayName);
        if (existingRepresentationOverlay != null)
            Object.DestroyImmediate(existingRepresentationOverlay.gameObject);

        Transform existingClassifierOverlay = canvas.Find(ClassifierOverlayName);
        if (existingClassifierOverlay != null)
            Object.DestroyImmediate(existingClassifierOverlay.gameObject);

        Transform existingCueOverlay = canvas.Find(CueOverlayName);
        if (existingCueOverlay != null)
            Object.DestroyImmediate(existingCueOverlay.gameObject);

        Transform existingScoreOverlay = canvas.Find(ScoreOverlayName);
        if (existingScoreOverlay != null)
            Object.DestroyImmediate(existingScoreOverlay.gameObject);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        TMP_FontAsset font = ResolveFont(scene);

        GameObject menu = CreateUIObject(MenuName, panel);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.anchorMin = menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = new Vector2(700f, 900f);

        Image background = menu.AddComponent<Image>();
        background.color = new Color(0.075f, 0.095f, 0.13f, 0.97f);
        background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = Image.Type.Sliced;

        VerticalLayoutGroup layout = menu.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(38, 38, 22, 22);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("TitleText", menu.transform, "EXPERIMENTO 1", 31f, FontStyles.Bold, TextAlignmentOptions.Center, 44f, font);

        CreateText("ParticipantLabel", menu.transform, "Subject ID", 18f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        TMP_InputField participant = CreateInputField("ParticipantInput", menu.transform, "LR102", 42f, font);

        CreateText("SessionLabel", menu.transform, "Sessão", 18f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        TMP_InputField session = CreateInputField("SessionInput", menu.transform, "1", 42f, font);
        session.contentType = TMP_InputField.ContentType.Standard;

        CreateText("OutputFolderLabel", menu.transform, "Pasta raiz dos dados", 18f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        TMP_Text outputPath = CreateText(
            "OutputPathText",
            menu.transform,
            "Destino: selecione Dados e informe Subject ID / sessão",
            14f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            72f,
            font);
        Button chooseFolder = CreateButton("ChooseFolderButton", menu.transform, "SELECIONAR PASTA DADOS", 40f, font);

        CreateText("OnlineBlockLabel", menu.transform, "Bloco / condição", 18f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        Button onlineBlock = CreateButton(
            "OnlineBlockButton",
            menu.transform,
            "BLOCO: TREINO (EM/IM)",
            44f,
            font,
            new Color(0.24f, 0.34f, 0.48f, 1f));
        TMP_Text onlineBlockText = onlineBlock.GetComponentInChildren<TMP_Text>();

        // Botão de câmera em uma seção própria, visualmente separada das
        // opções experimentais para evitar confusão com o bloco.
        GameObject cameraSection = CreateUIObject("CameraSection", menu.transform);
        LayoutElement cameraSectionLayout = cameraSection.AddComponent<LayoutElement>();
        cameraSectionLayout.preferredHeight = 68f;

        Image cameraSectionBackground = cameraSection.AddComponent<Image>();
        cameraSectionBackground.color = new Color(0.055f, 0.070f, 0.095f, 0.72f);
        cameraSectionBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cameraSectionBackground.type = Image.Type.Sliced;

        RectTransform cameraSectionRect = cameraSection.GetComponent<RectTransform>();

        Button cameraAdjust = CreateButton(
            "CameraAdjustButton",
            cameraSection.transform,
            "AJUSTAR CÂMERA",
            44f,
            font,
            new Color(0.20f, 0.38f, 0.56f, 1f));

        RectTransform cameraAdjustRect = cameraAdjust.GetComponent<RectTransform>();
        cameraAdjustRect.anchorMin = Vector2.zero;
        cameraAdjustRect.anchorMax = Vector2.one;
        cameraAdjustRect.offsetMin = new Vector2(10f, 10f);
        cameraAdjustRect.offsetMax = new Vector2(-10f, -10f);

        LayoutElement cameraAdjustLayout = cameraAdjust.GetComponent<LayoutElement>();
        if (cameraAdjustLayout != null)
            Object.DestroyImmediate(cameraAdjustLayout);

        TMP_Text eegStatus = CreateText("DecoderStatusText", menu.transform, "BCI: aguardando Signal", 16f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        TMP_Text markerStatus = CreateText("MarkerStatusText", menu.transform, "PsychoPy: aguardando GrazMI_Markers", 16f, FontStyles.Normal, TextAlignmentOptions.Left, 22f, font);
        Button start = CreateButton("StartButton", menu.transform, "INICIAR EXPERIMENTO", 52f, font);
        Button back = CreateButton("BackButton", menu.transform, "VOLTAR", 40f, font, new Color(0.27f, 0.31f, 0.38f, 1f));

        // HUD compacto no topo: não ocupa a área dos sliders.
        GameObject cameraOverlay = CreateUIObject(CameraOverlayName, canvas);
        RectTransform cameraOverlayRect = cameraOverlay.GetComponent<RectTransform>();
        cameraOverlayRect.anchorMin = cameraOverlayRect.anchorMax = new Vector2(0.5f, 1f);
        cameraOverlayRect.pivot = new Vector2(0.5f, 1f);
        cameraOverlayRect.anchoredPosition = new Vector2(0f, -16f);
        cameraOverlayRect.sizeDelta = new Vector2(940f, 68f);

        Image cameraOverlayBackground = cameraOverlay.AddComponent<Image>();
        cameraOverlayBackground.color = new Color(0.05f, 0.07f, 0.10f, 0.86f);
        cameraOverlayBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cameraOverlayBackground.type = Image.Type.Sliced;

        HorizontalLayoutGroup cameraLayout = cameraOverlay.AddComponent<HorizontalLayoutGroup>();
        cameraLayout.padding = new RectOffset(18, 18, 10, 10);
        cameraLayout.spacing = 14f;
        cameraLayout.childAlignment = TextAnchor.MiddleCenter;
        cameraLayout.childControlWidth = true;
        cameraLayout.childControlHeight = true;
        cameraLayout.childForceExpandWidth = false;
        cameraLayout.childForceExpandHeight = true;

        TMP_Text cameraValues = CreateText(
            "CameraValuesText",
            cameraOverlay.transform,
            "Rot -- | H -- | V -- | FOV --",
            16f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft,
            46f,
            font);

        LayoutElement cameraValuesLayout = cameraValues.GetComponent<LayoutElement>();
        cameraValuesLayout.preferredWidth = 390f;
        cameraValuesLayout.flexibleWidth = 1f;

        TMP_Text fovLabel = CreateText(
            "FovLabel",
            cameraOverlay.transform,
            "FOV",
            16f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            46f,
            font);

        LayoutElement fovLabelLayout = fovLabel.GetComponent<LayoutElement>();
        fovLabelLayout.preferredWidth = 48f;
        fovLabelLayout.flexibleWidth = 0f;

        Slider cameraFov = CreateSlider(
            "CameraFovSlider",
            cameraOverlay.transform,
            45f,
            95f,
            76f,
            230f);

        Button cameraDone = CreateButton(
            "CameraDoneButton",
            cameraOverlay.transform,
            "CONCLUIR",
            46f,
            font,
            new Color(0.18f, 0.45f, 0.72f, 1f));

        LayoutElement cameraDoneLayout = cameraDone.GetComponent<LayoutElement>();
        cameraDoneLayout.preferredWidth = 180f;
        cameraDoneLayout.flexibleWidth = 0f;

        cameraOverlay.SetActive(false);

        // -----------------------------------------------------------------
        // Feedback contínuo do classificador (predict_proba).
        // O PCA continua chegando em Signal/BCI, mas não é desenhado no Unity.
        // Nesta versão, o feedback fica mais central/baixo e usa barras
        // verticais alinhadas espacialmente: esquerda, centro (REST), direita.
        // -----------------------------------------------------------------
        GameObject classifierOverlay = CreateUIObject(ClassifierOverlayName, canvas);
        RectTransform classifierOverlayRect = classifierOverlay.GetComponent<RectTransform>();
        classifierOverlayRect.anchorMin = classifierOverlayRect.anchorMax = new Vector2(0.5f, 1f);
        classifierOverlayRect.pivot = new Vector2(0.5f, 1f);
        classifierOverlayRect.anchoredPosition = new Vector2(0f, -72f);
        classifierOverlayRect.sizeDelta = new Vector2(470f, 390f);

        Image classifierBackground = classifierOverlay.AddComponent<Image>();
        classifierBackground.color = new Color(0.025f, 0.035f, 0.055f, 0.95f);
        classifierBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        classifierBackground.type = Image.Type.Sliced;
        classifierBackground.raycastTarget = false;

        TMP_Text classifierTitle = CreateText(
            "ClassifierTitle",
            classifierOverlay.transform,
            "CLASSIFICAÇÃO CONTÍNUA",
            15f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            24f,
            font);
        RectTransform classifierTitleRect = classifierTitle.GetComponent<RectTransform>();
        classifierTitleRect.anchorMin = new Vector2(0f, 1f);
        classifierTitleRect.anchorMax = new Vector2(1f, 1f);
        classifierTitleRect.pivot = new Vector2(0.5f, 1f);
        classifierTitleRect.anchoredPosition = new Vector2(0f, -8f);
        classifierTitleRect.sizeDelta = new Vector2(-24f, 18f);

        TMP_Text classifierStatus = CreateText(
            "ClassifierStatus",
            classifierOverlay.transform,
            "CLASSIF.: --    |    CONTROLE: REST",
            11f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            20f,
            font);
        RectTransform classifierStatusRect = classifierStatus.GetComponent<RectTransform>();
        classifierStatusRect.anchorMin = new Vector2(0f, 1f);
        classifierStatusRect.anchorMax = new Vector2(1f, 1f);
        classifierStatusRect.pivot = new Vector2(0.5f, 1f);
        classifierStatusRect.anchoredPosition = new Vector2(0f, -26f);
        classifierStatusRect.sizeDelta = new Vector2(-24f, 16f);

        GameObject classifierBars = CreateUIObject("ProbabilityBars", classifierOverlay.transform);
        RectTransform classifierBarsRect = classifierBars.GetComponent<RectTransform>();
        classifierBarsRect.anchorMin = new Vector2(0f, 0f);
        classifierBarsRect.anchorMax = new Vector2(1f, 1f);
        classifierBarsRect.offsetMin = new Vector2(16f, 10f);
        classifierBarsRect.offsetMax = new Vector2(-16f, -48f);

        HorizontalLayoutGroup probabilityLayout = classifierBars.AddComponent<HorizontalLayoutGroup>();
        probabilityLayout.padding = new RectOffset(4, 4, 0, 0);
        probabilityLayout.spacing = 12f;
        probabilityLayout.childAlignment = TextAnchor.LowerCenter;
        probabilityLayout.childControlWidth = true;
        probabilityLayout.childControlHeight = true;
        probabilityLayout.childForceExpandWidth = true;
        probabilityLayout.childForceExpandHeight = true;

        CreateProbabilityColumn(classifierBars.transform, "Left", "ESQ.", font,
            new Color(0.20f, 0.55f, 0.95f, 1f), out CanvasGroup leftGroup,
            out Image leftFill, out TMP_Text leftValue);
        CreateProbabilityColumn(classifierBars.transform, "Rest", "REST", font,
            new Color(0.20f, 0.55f, 0.95f, 1f), out CanvasGroup restGroup,
            out Image restFill, out TMP_Text restValue);
        CreateProbabilityColumn(classifierBars.transform, "Right", "DIR.", font,
            new Color(0.20f, 0.55f, 0.95f, 1f), out CanvasGroup rightGroup,
            out Image rightFill, out TMP_Text rightValue);

        ClassifierProbabilityView classifierView = classifierOverlay.AddComponent<ClassifierProbabilityView>();
        classifierView.root = classifierOverlay;
        classifierView.leftFill = leftFill;
        classifierView.restFill = restFill;
        classifierView.rightFill = rightFill;
        classifierView.leftValueText = leftValue;
        classifierView.restValueText = restValue;
        classifierView.rightValueText = rightValue;
        classifierView.leftGroup = leftGroup;
        classifierView.restGroup = restGroup;
        classifierView.rightGroup = rightGroup;
        classifierView.statusText = classifierStatus;
        classifierView.visualSmoothing = 12f;

        classifierOverlay.SetActive(false);

        // -----------------------------------------------------------------
        // Indicador espacial do cue PsychoPy.
        // Círculo vazio = planejamento; círculo cheio = execução.
        // Usado no TREINO e no ONLINE COM FEEDBACK; não há texto esquerda/direita.
        // -----------------------------------------------------------------
        GameObject cueOverlay = CreateUIObject(CueOverlayName, canvas);
        RectTransform cueOverlayRect = cueOverlay.GetComponent<RectTransform>();
        cueOverlayRect.anchorMin = Vector2.zero;
        cueOverlayRect.anchorMax = Vector2.one;
        cueOverlayRect.pivot = new Vector2(0.5f, 0.5f);
        cueOverlayRect.offsetMin = Vector2.zero;
        cueOverlayRect.offsetMax = Vector2.zero;

        Canvas cueCanvas = cueOverlay.AddComponent<Canvas>();
        cueCanvas.overrideSorting = true;
        cueCanvas.sortingOrder = 5000;

        CanvasGroup cueGroup = cueOverlay.AddComponent<CanvasGroup>();
        cueGroup.alpha = 1f;
        cueGroup.interactable = false;
        cueGroup.blocksRaycasts = false;

        Image cueBackground = cueOverlay.AddComponent<Image>();
        cueBackground.color = Color.clear;
        cueBackground.raycastTarget = false;

        GameObject cueIndicatorObject = CreateUIObject("CueCommandText", cueOverlay.transform);
        TextMeshProUGUI cueText = cueIndicatorObject.AddComponent<TextMeshProUGUI>();
        cueText.text = "○";
        cueText.font = font;
        cueText.fontSize = 96f;
        cueText.fontStyle = FontStyles.Bold;
        cueText.alignment = TextAlignmentOptions.Center;
        cueText.color = Color.white;
        cueText.enableWordWrapping = false;
        cueText.raycastTarget = false;

        RectTransform cueTextRect = cueText.GetComponent<RectTransform>();
        cueTextRect.anchorMin = cueTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        cueTextRect.pivot = new Vector2(0.5f, 0.5f);
        cueTextRect.anchoredPosition = new Vector2(-170f, -155f);
        cueTextRect.sizeDelta = new Vector2(180f, 180f);

        GameObject cuePhaseObject = CreateUIObject("CuePhaseText", cueOverlay.transform);
        TextMeshProUGUI cuePhaseText = cuePhaseObject.AddComponent<TextMeshProUGUI>();
        cuePhaseText.text = "PREPARE";
        cuePhaseText.font = font;
        cuePhaseText.fontSize = 22f;
        cuePhaseText.fontStyle = FontStyles.Bold;
        cuePhaseText.alignment = TextAlignmentOptions.Center;
        cuePhaseText.color = Color.white;
        cuePhaseText.enableWordWrapping = false;
        cuePhaseText.raycastTarget = false;

        RectTransform cuePhaseRect = cuePhaseText.GetComponent<RectTransform>();
        cuePhaseRect.anchorMin = cuePhaseRect.anchorMax = new Vector2(0.5f, 0.5f);
        cuePhaseRect.pivot = new Vector2(0.5f, 0.5f);
        cuePhaseRect.anchoredPosition = new Vector2(-170f, -237f);
        cuePhaseRect.sizeDelta = new Vector2(180f, 34f);

        cueOverlay.SetActive(false);

        // -----------------------------------------------------------------
        // Placar do Game 1. Substitui os pop-ups de acerto/erro nos modos
        // que usam bolas.
        // -----------------------------------------------------------------
        GameObject scoreOverlay = CreateUIObject(ScoreOverlayName, canvas);
        RectTransform scoreRect = scoreOverlay.GetComponent<RectTransform>();
        scoreRect.anchorMin = scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(28f, -24f);
        scoreRect.sizeDelta = new Vector2(310f, 58f);

        Canvas scoreCanvas = scoreOverlay.AddComponent<Canvas>();
        scoreCanvas.overrideSorting = true;
        scoreCanvas.sortingOrder = 4900;

        CanvasGroup scoreGroup = scoreOverlay.AddComponent<CanvasGroup>();
        scoreGroup.alpha = 1f;
        scoreGroup.interactable = false;
        scoreGroup.blocksRaycasts = false;

        Image scoreBackground = scoreOverlay.AddComponent<Image>();
        scoreBackground.color = new Color(0.025f, 0.035f, 0.055f, 0.96f);
        scoreBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        scoreBackground.type = Image.Type.Sliced;
        scoreBackground.raycastTarget = false;

        TMP_Text scoreText = CreateText(
            "ScoreText",
            scoreOverlay.transform,
            "ACERTOS  0   |   ERROS  0",
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            48f,
            font);

        RectTransform scoreTextRect = scoreText.GetComponent<RectTransform>();
        scoreTextRect.anchorMin = Vector2.zero;
        scoreTextRect.anchorMax = Vector2.one;
        scoreTextRect.offsetMin = new Vector2(14f, 4f);
        scoreTextRect.offsetMax = new Vector2(-14f, -4f);

        scoreOverlay.SetActive(false);

        PsychoPyMarkerReceiver receiver = canvas.GetComponent<PsychoPyMarkerReceiver>();
        if (receiver == null)
            receiver = canvas.gameObject.AddComponent<PsychoPyMarkerReceiver>();

        BCIDecoderReceiver decoderReceiver = canvas.GetComponent<BCIDecoderReceiver>();
        if (decoderReceiver == null)
            decoderReceiver = canvas.gameObject.AddComponent<BCIDecoderReceiver>();

        BCIControlReceiver controlReceiver = canvas.GetComponent<BCIControlReceiver>();
        if (controlReceiver == null)
            controlReceiver = canvas.gameObject.AddComponent<BCIControlReceiver>();

        Game1OutcomeOutlet outcomeOutlet = canvas.GetComponent<Game1OutcomeOutlet>();
        if (outcomeOutlet == null)
            outcomeOutlet = canvas.gameObject.AddComponent<Game1OutcomeOutlet>();

        Game1SimpleMenuController controller = canvas.GetComponent<Game1SimpleMenuController>();
        if (controller == null)
            controller = canvas.gameObject.AddComponent<Game1SimpleMenuController>();

        controller.menuRoot = menu;
        controller.participantInput = participant;
        controller.sessionInput = session;
        controller.outputPathText = outputPath;
        controller.eegStatusText = eegStatus;
        controller.markerStatusText = markerStatus;
        controller.chooseFolderButton = chooseFolder;
        controller.onlineBlockButton = onlineBlock;
        controller.onlineBlockButtonText = onlineBlockText;
        controller.onlineBlock = Game1OnlineBlock.Treino;
        controller.experimentType = "BMI";
        controller.onlineFolderName = "online";
        // O botão de bloco é a única seleção de condição.
        controller.feedbackModeButton = null;
        controller.feedbackModeButtonText = null;
        controller.chooseRepresentationMapButton = null;
        controller.representationMapPathText = null;
        controller.representationView = null;
        controller.classifierFeedbackView = classifierView;
        controller.classifierFeedbackPosition = new Vector2(0f, -72f);
        controller.classifierFeedbackSize = new Vector2(470f, 390f);
        controller.cueCommandRoot = cueOverlay;
        controller.cueCommandText = cueText;
        controller.cuePhaseText = cuePhaseText;
        controller.cueLeftPosition = new Vector2(-170f, -155f);
        controller.cueCenterPosition = new Vector2(0f, -155f);
        controller.cueRightPosition = new Vector2(170f, -155f);
        controller.cuePlanningFontSize = 96f;
        controller.cueExecutionFontSize = 78f;
        controller.scoreRoot = scoreOverlay;
        controller.scoreText = scoreText;
        controller.cameraAdjustButton = cameraAdjust;
        controller.startButton = start;
        controller.backButton = back;
        controller.cameraAdjustRoot = cameraOverlay;
        controller.cameraValuesText = cameraValues;
        controller.cameraFovSlider = cameraFov;
        controller.cameraDoneButton = cameraDone;

        // Integração com o pipeline Python/PsychoPy já existente.
        controller.decoderStreamName = "Signal";
        controller.decoderStreamType = "BCI";
        controller.markerStreamName = "GrazMI_Markers";
        controller.markerStreamType = "Markers";
        controller.controlStreamName = "GrazMI_Control";
        controller.controlStreamType = "BCIControl";
        controller.decoderThreshold = 0.60f;
        controller.continuousDecoderAmplitude = false;
        controller.allowBothClass = true;
        // O bloco define o modo:
        // sem_feedback -> avatar + bolas
        // feedback     -> avatar + probabilidades + indicador espacial
        controller.feedbackMode = Game1FeedbackMode.AvatarCueTraining;
        controller.representationMapPositionWithBalls = new Vector2(-36f, -14f);
        controller.representationMapPositionWithCommands = new Vector2(0f, -14f);
        controller.representationMapSize = new Vector2(700f, 300f);
        controller.cueCommandPosition = new Vector2(0f, 55f);
        controller.cueCommandSize = new Vector2(700f, 76f);

        controller.sendUnityFeedback = false;
        controller.feedbackStreamName = "Unity_Game1_Feedback";
        controller.feedbackStreamType = "Markers";

        controller.ballArrivalTime = 2.5f;
        controller.ballSpeed = 3f;
        controller.trajectoryMin = new Vector2(-25f, 5f);
        controller.trajectoryMax = new Vector2(25f, 25f);
        controller.trajectoryBufferSize = 256;

        // Padrão específico do Game 1.
        controller.applyDefaultCameraView = true;
        controller.defaultCameraRotation = 45f;
        controller.defaultCameraHorizontal = 0.51f;
        controller.defaultCameraVertical = 0.54f;
        controller.defaultCameraFov = 76f;
        controller.minCameraFov = 45f;
        controller.maxCameraFov = 95f;

        EditorUtility.SetDirty(receiver);
        EditorUtility.SetDirty(decoderReceiver);
        EditorUtility.SetDirty(controlReceiver);
        EditorUtility.SetDirty(outcomeOutlet);
        EditorUtility.SetDirty(classifierView);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = menu;

        Debug.Log("Menu simples do Game 1 criado e salvo em Assets/Scenes/Game UI.unity.");
    }

    [MenuItem("Tools/UNIFESP/Restaurar menu antigo do Game 1")]
    public static void Restore()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform panel = FindInScene(scene, "Painel de configuracao");
        Transform canvas = FindInScene(scene, "Canvas");

        if (panel == null)
            return;

        Transform menu = panel.Find(MenuName);
        if (menu != null)
            Object.DestroyImmediate(menu.gameObject);

        if (canvas != null)
        {
            Transform cameraOverlay = canvas.Find(CameraOverlayName);
            if (cameraOverlay != null)
                Object.DestroyImmediate(cameraOverlay.gameObject);

            Transform representationOverlay = canvas.Find(RepresentationOverlayName);
            if (representationOverlay != null)
                Object.DestroyImmediate(representationOverlay.gameObject);

            Transform classifierOverlay = canvas.Find(ClassifierOverlayName);
            if (classifierOverlay != null)
                Object.DestroyImmediate(classifierOverlay.gameObject);

            Transform cueOverlay = canvas.Find(CueOverlayName);
            if (cueOverlay != null)
                Object.DestroyImmediate(cueOverlay.gameObject);

            Transform scoreOverlay = canvas.Find(ScoreOverlayName);
            if (scoreOverlay != null)
                Object.DestroyImmediate(scoreOverlay.gameObject);
        }

        SetActiveIfFound(panel, "Pages", true);
        SetActiveIfFound(panel, "Tabs", true);
        SetActiveIfFound(panel, "Background", true);
        SetActiveIfFound(panel, "Fechar", true);

        if (canvas != null)
        {
            Game1SimpleMenuController controller = canvas.GetComponent<Game1SimpleMenuController>();
            PsychoPyMarkerReceiver receiver = canvas.GetComponent<PsychoPyMarkerReceiver>();
            BCIDecoderReceiver decoderReceiver = canvas.GetComponent<BCIDecoderReceiver>();
            BCIControlReceiver controlReceiver = canvas.GetComponent<BCIControlReceiver>();
            Game1OutcomeOutlet outcomeOutlet = canvas.GetComponent<Game1OutcomeOutlet>();
            if (controller != null) Object.DestroyImmediate(controller);
            if (receiver != null) Object.DestroyImmediate(receiver);
            if (decoderReceiver != null) Object.DestroyImmediate(decoderReceiver);
            if (controlReceiver != null) Object.DestroyImmediate(controlReceiver);
            if (outcomeOutlet != null) Object.DestroyImmediate(outcomeOutlet);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Menu antigo do Game 1 restaurado.");
    }

    private static Transform FindInScene(Scene scene, string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(t => t.gameObject.scene == scene && t.name == objectName);
    }

    private static TMP_FontAsset ResolveFont(Scene scene)
    {
        TMP_Text existing = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(t => t.gameObject.scene == scene && t.font != null);

        return existing != null ? existing.font : TMP_Settings.defaultFontAsset;
    }

    private static void SetActiveIfFound(Transform parent, string name, bool active)
    {
        Transform child = parent.Find(name);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return obj;
    }


    private static void CreateProbabilityColumn(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font,
        Color fillColor,
        out CanvasGroup group,
        out Image fill,
        out TMP_Text valueText)
    {
        GameObject column = CreateUIObject(name + "ProbabilityColumn", parent);
        group = column.AddComponent<CanvasGroup>();

        // O HorizontalLayoutGroup do pai controla o tamanho da coluna.
        // Dentro dela usamos RectTransforms manuais para garantir que a barra
        // ocupe quase toda a altura disponível, independentemente do LayoutGroup.
        LayoutElement columnLayout = column.AddComponent<LayoutElement>();
        columnLayout.preferredWidth = 100f;
        columnLayout.flexibleWidth = 1f;

        RectTransform columnRect = column.GetComponent<RectTransform>();

        valueText = CreateInnerText(
            name + "Value",
            column.transform,
            "0%",
            Color.white,
            font);
        valueText.fontSize = 14f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Center;
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 1f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(0.5f, 1f);
        valueRect.anchoredPosition = new Vector2(0f, -2f);
        valueRect.sizeDelta = new Vector2(0f, 24f);

        TMP_Text labelText = CreateInnerText(
            name + "Label",
            column.transform,
            label,
            Color.white,
            font);
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
        labelRect.sizeDelta = new Vector2(0f, 20f);

        GameObject barBackgroundObject = CreateUIObject(name + "BarBackground", column.transform);
        RectTransform barRect = barBackgroundObject.GetComponent<RectTransform>();

        // Barra estreita, mas com stretch vertical: ocupa toda a coluna
        // entre o percentual de cima e o rótulo de baixo.
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.offsetMin = new Vector2(-32f, 26f);
        barRect.offsetMax = new Vector2(32f, -30f);

        Image barBackground = barBackgroundObject.AddComponent<Image>();
        barBackground.color = new Color(1f, 1f, 1f, 0.16f);
        barBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        barBackground.type = Image.Type.Sliced;
        barBackground.raycastTarget = false;

        GameObject fillObject = CreateUIObject(name + "Fill", barBackgroundObject.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        Stretch(fillRect, 4f, 4f, 4f, 4f);

        fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = 0;     // cresce de baixo para cima
        fill.fillAmount = 0f;    // predict_proba 0..1
        fill.raycastTarget = false;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        float height,
        TMP_FontAsset font)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        LayoutElement element = obj.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        return text;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText, float height, TMP_FontAsset font)
    {
        GameObject root = CreateUIObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.96f);
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        image.type = Image.Type.Sliced;

        LayoutElement element = root.AddComponent<LayoutElement>();
        element.preferredHeight = height;

        GameObject viewport = CreateUIObject("Text Area", root.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 14f, 12f, 14f, 12f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateInnerText("Placeholder", viewport.transform, placeholderText, new Color(0.25f, 0.25f, 0.25f, 0.55f), font);
        TextMeshProUGUI text = CreateInnerText("Text", viewport.transform, string.Empty, new Color(0.08f, 0.08f, 0.08f, 1f), font);

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 30;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        return input;
    }

    private static TextMeshProUGUI CreateInnerText(string name, Transform parent, string value, Color color, TMP_FontAsset font)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        Stretch(rect, 0f, 0f, 0f, 0f);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = 21f;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, float height, TMP_FontAsset font, Color? color = null)
    {
        GameObject root = CreateUIObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.color = color ?? new Color(0.18f, 0.45f, 0.72f, 1f);
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;

        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.84f, 0.95f, 1f);
        button.colors = colors;

        LayoutElement element = root.AddComponent<LayoutElement>();
        element.preferredHeight = height;

        GameObject textObject = CreateUIObject("Text (TMP)", root.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect, 8f, 4f, 8f, 4f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.font = font;
        text.fontSize = 19f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private static Slider CreateSlider(
        string name,
        Transform parent,
        float minValue,
        float maxValue,
        float value,
        float width)
    {
        GameObject root = CreateUIObject(name, parent);
        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredWidth = width;
        rootLayout.preferredHeight = 46f;
        rootLayout.flexibleWidth = 0f;

        Slider slider = root.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject background = CreateUIObject("Background", root.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);

        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.24f, 0.28f, 0.34f, 1f);
        backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        backgroundImage.type = Image.Type.Sliced;

        GameObject fillArea = CreateUIObject("Fill Area", root.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(5f, -4f);
        fillAreaRect.offsetMax = new Vector2(-5f, 4f);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect, 0f, 0f, 0f, 0f);

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.26f, 0.58f, 0.88f, 1f);
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;

        GameObject handleArea = CreateUIObject("Handle Slide Area", root.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect, 8f, 0f, 8f, 0f);

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);

        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
#endif
