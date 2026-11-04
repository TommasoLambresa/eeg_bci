using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;

public class PhotoMode : MonoBehaviour
{
    [HideInInspector] public bool modeActive = false; // Flag do modo ativo

    [Header("Path"), Space]
    [SerializeField] private string _pathFolder; // Pasta de screenshots
    [SerializeField] private string _folderName = "Screenshots"; // Nome da pasta
    [SerializeField] private TMP_Text _pathMesh; // Texto do caminho

    [Space, Header("Canvas Group"), Space]
    public CanvasGroup configsGroup; // Grupo de configura��es
    public CanvasGroup iniciarGroup; // Grupo do iniciar
    public CanvasGroup statusGroup; // Grupo do status

    // Objetos do modo foto
    public GameObject btnPhoto;
    public GameObject zoomOBJ;
    public GameObject inputLeft;
    public GameObject inputRight;

    private TMP_Text camRotationText;
    private TMP_Text camHorizontalText;
    private TMP_Text camVerticalText;
    private TMP_Text camZoomText;
    private TMP_Text inputLeftText;
    private TMP_Text inputRightText;
    private TMP_Text lastSavedText;
    private Canvas screenshotToastCanvas;
    private TMP_Text screenshotToastText;
    private Coroutine screenshotToastCoroutine;
    private bool readoutsCached;
    private PhotoModeFeedbackOverlay feedbackOverlay;
    private RectTransform photoControlsRect;
    private GameObject photoControlsToggleObject;
    private TMP_Text photoControlsToggleText;
    private bool photoControlsExpanded = true;
    private bool photoControlsLayoutCached;
    private Vector2 originalControlsAnchorMin;
    private Vector2 originalControlsAnchorMax;
    private Vector2 originalControlsPivot;
    private Vector2 originalControlsAnchoredPosition;
    private Vector2 originalControlsSizeDelta;

    void Start()
    {
        // Carrega caminho salvo
        if (PlayerPrefs.HasKey("PathFolder"))
        {
            _pathFolder = PlayerPrefs.GetString("PathFolder");
            _pathMesh.text = _pathFolder;
        }
        else
        {
            // Define um caminho padro para evitar que o boto fique travado
            _pathFolder = Path.Combine(Application.persistentDataPath, _folderName);
            _pathMesh.text = _pathFolder;
            PlayerPrefs.SetString("PathFolder", _pathFolder);
        }

        CacheReadoutLabels();
        EnsureFeedbackOverlay();
    }

    private void Update()
    {
        if (modeActive)
        {
            UpdateReadouts();
        }
        else if (feedbackOverlay != null)
        {
            feedbackOverlay.SetOverlayActive(false);
        }
    }

    // Entra no modo foto
    public void EntryPhotoMode()
    {
        // Se ainda estiver vazio por algum motivo, garante o caminho padro
        if (string.IsNullOrEmpty(_pathFolder))
        {
            _pathFolder = Path.Combine(Application.persistentDataPath, _folderName);
            _pathMesh.text = _pathFolder;
        }

        UIConfigs.instance.configsUI.SetActive(true);
        btnPhoto.SetActive(true);
        inputLeft.SetActive(true);
        inputRight.SetActive(true);
        zoomOBJ.SetActive(false);
        configsGroup.alpha = 0;
        iniciarGroup.alpha = 0;
        modeActive = true;
        CacheReadoutLabels();
        EnsureFeedbackOverlay();
        ApplyPhotoControlsLayout();
        SetPhotoControlsExpanded(true);
        feedbackOverlay.SetOverlayActive(true);
        UpdateReadouts();
    }

    // Troca entre camera lateral e padr�o
    public void SwitchCam() => PlayerController.instance.SwitchCam();

    // Sai do modo foto
    public void LeavePhotoMode()
    {
        btnPhoto.SetActive(false);
        inputLeft.SetActive(false);
        inputRight.SetActive(false);
        zoomOBJ.SetActive(true);
        UIConfigs.instance.configsUI.SetActive(false);
        modeActive = false;
        ResetReadouts();
        RestorePhotoControlsLayout();
        if (feedbackOverlay != null)
            feedbackOverlay.SetOverlayActive(false);

        // Volta para a camera padr�o
        var controller = PlayerController.instance;
        controller.mainCam.gameObject.SetActive(true);
        controller.lateralCam.gameObject.SetActive(false);

        var redesignedMenu = GetComponent<Experiment2RedesignedMenu>();
        if (redesignedMenu != null)
        {
            HideCanvasGroup(configsGroup);
            HideCanvasGroup(iniciarGroup);
            redesignedMenu.ShowAfterPhotoMode();
        }
        else
        {
            ShowCanvasGroup(configsGroup);
            ShowCanvasGroup(iniciarGroup);
        }
    }

    private void HideCanvasGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void ShowCanvasGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    // Tira um print
    public void TakeScreenshot()
    {
        if (!Directory.Exists(_pathFolder))
            Directory.CreateDirectory(_pathFolder);

        StartCoroutine(ScreenshotCoroutine());
    }

    // Corrotina para tirar o print
    private IEnumerator ScreenshotCoroutine()
    {
        float previousConfigsAlpha = configsGroup.alpha;
        float previousIniciarAlpha = iniciarGroup.alpha;
        float previousStatusAlpha = statusGroup.alpha;

        // Esconde os grupos de UI que nao devem aparecer na foto.
        configsGroup.alpha = 0;
        iniciarGroup.alpha = 0;
        statusGroup.alpha = 0;
        if (feedbackOverlay != null)
            feedbackOverlay.SetControlsVisible(false);
        SetPhotoControlsToggleVisible(false);

        yield return new WaitForEndOfFrame();

        string fileName = $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string filePath = Path.Combine(_pathFolder, fileName);

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(filePath, screenshot.EncodeToPNG());
        Destroy(screenshot);

        // Restaura exatamente o estado anterior do modo foto.
        configsGroup.alpha = previousConfigsAlpha;
        iniciarGroup.alpha = previousIniciarAlpha;
        statusGroup.alpha = previousStatusAlpha;
        if (feedbackOverlay != null)
            feedbackOverlay.SetControlsVisible(modeActive);
        SetPhotoControlsToggleVisible(modeActive);

        if (lastSavedText != null)
            lastSavedText.text = $"Captura de tela salva: {fileName}";
        ShowScreenshotToast($"Captura de tela salva: {fileName}");
    }

    // Seleciona pasta
    public void SelecionarPath()
    {
        FileBrowser.SetFilters(false);
        StartCoroutine(SelectPathCoroutine());
    }

    [HideInInspector] public Dictionary<string, List<string>> pathDataInfos;

    // Corrotina para selecionar pasta
    private IEnumerator SelectPathCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, null, null, "Selecione o caminho", "Selecionar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
            {
                _pathFolder = OnFilesSelected(FileBrowser.Result);
            }

            _pathFolder = Path.Combine(_pathFolder, _folderName);
            _pathMesh.text = _pathFolder;

            PlayerPrefs.SetString("PathFolder", _pathFolder);
        }
    }

    private string OnFilesSelected(string[] filePaths)
    {
        string filePath = filePaths[0];

        return filePath;
    }

    private void CacheReadoutLabels()
    {
        if (readoutsCached)
            return;

        var status = UIConfigs.instance != null ? UIConfigs.instance.statusUI : null;
        if (status != null && status.configs != null)
        {
            camRotationText = FindTextContaining(status.configs, "Rot");
            camHorizontalText = FindTextContaining(status.configs, "horizontal");
            camVerticalText = FindTextContaining(status.configs, "vertical");
            camZoomText = FindTextContaining(status.configs, "zoom");
        }

        inputLeftText = FindTextContaining(inputLeft, "esquerdo");
        inputRightText = FindTextContaining(inputRight, "direito");
        lastSavedText = _pathMesh;
        readoutsCached = true;
    }

    private void EnsureFeedbackOverlay()
    {
        if (feedbackOverlay == null)
            feedbackOverlay = GetComponent<PhotoModeFeedbackOverlay>();
        if (feedbackOverlay == null)
            feedbackOverlay = gameObject.AddComponent<PhotoModeFeedbackOverlay>();
    }

    private void ApplyPhotoControlsLayout()
    {
        var status = UIConfigs.instance != null ? UIConfigs.instance.statusUI : null;
        if (status == null || status.configs == null)
            return;

        photoControlsRect = status.GetComponent<RectTransform>();
        if (photoControlsRect == null)
            return;

        if (!photoControlsLayoutCached)
        {
            originalControlsAnchorMin = photoControlsRect.anchorMin;
            originalControlsAnchorMax = photoControlsRect.anchorMax;
            originalControlsPivot = photoControlsRect.pivot;
            originalControlsAnchoredPosition = photoControlsRect.anchoredPosition;
            originalControlsSizeDelta = photoControlsRect.sizeDelta;
            photoControlsLayoutCached = true;
        }

        photoControlsRect.anchorMin = new Vector2(1f, 0f);
        photoControlsRect.anchorMax = new Vector2(1f, 0f);
        photoControlsRect.pivot = new Vector2(1f, 0f);
        photoControlsRect.anchoredPosition = new Vector2(-18f, 134f);

        EnsurePhotoControlsToggle();
        SetPhotoControlsToggleVisible(true);
    }

    private void RestorePhotoControlsLayout()
    {
        SetPhotoControlsToggleVisible(false);

        if (photoControlsRect == null || !photoControlsLayoutCached)
            return;

        photoControlsRect.anchorMin = originalControlsAnchorMin;
        photoControlsRect.anchorMax = originalControlsAnchorMax;
        photoControlsRect.pivot = originalControlsPivot;
        photoControlsRect.anchoredPosition = originalControlsAnchoredPosition;
        photoControlsRect.sizeDelta = originalControlsSizeDelta;
    }

    private void EnsurePhotoControlsToggle()
    {
        if (photoControlsToggleObject != null)
            return;

        var parent = photoControlsRect != null && photoControlsRect.parent != null
            ? photoControlsRect.parent
            : UIConfigs.instance != null ? UIConfigs.instance.configsUI.transform : transform;
        photoControlsToggleObject = new GameObject("Photo Controls Toggle");
        photoControlsToggleObject.transform.SetParent(parent, false);
        photoControlsToggleObject.transform.SetAsLastSibling();

        var rect = photoControlsToggleObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-18f, 18f);
        rect.sizeDelta = new Vector2(178f, 38f);

        var image = photoControlsToggleObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.62f);
        image.raycastTarget = true;

        var button = photoControlsToggleObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(TogglePhotoControls);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(photoControlsToggleObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        photoControlsToggleText = textObject.AddComponent<TextMeshProUGUI>();
        photoControlsToggleText.alignment = TextAlignmentOptions.Center;
        photoControlsToggleText.fontSize = 16f;
        photoControlsToggleText.fontStyle = FontStyles.Bold;
        photoControlsToggleText.color = Color.white;
        photoControlsToggleText.raycastTarget = false;
    }

    private void TogglePhotoControls()
    {
        SetPhotoControlsExpanded(!photoControlsExpanded);
    }

    private void SetPhotoControlsExpanded(bool expanded)
    {
        photoControlsExpanded = expanded;

        var status = UIConfigs.instance != null ? UIConfigs.instance.statusUI : null;
        if (status != null && status.configs != null)
            status.configs.SetActive(expanded);

        if (photoControlsToggleText != null)
            photoControlsToggleText.text = expanded ? "Ocultar controles" : "Controles";
    }

    private void SetPhotoControlsToggleVisible(bool visible)
    {
        if (photoControlsToggleObject != null)
        {
            photoControlsToggleObject.SetActive(visible);
            if (visible)
                photoControlsToggleObject.transform.SetAsLastSibling();
        }
    }

    private TMP_Text FindTextContaining(GameObject root, string text)
    {
        if (root == null)
            return null;

        var labels = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var label in labels)
        {
            if (label.text.Contains(text))
                return label;
        }

        return null;
    }

    private void UpdateReadouts()
    {
        CacheReadoutLabels();

        var player = PlayerController.instance;
        var status = UIConfigs.instance != null ? UIConfigs.instance.statusUI : null;

        if (player != null)
        {
            float[] inputs = player.GetInputValues();
            float leftInput = inputs != null && inputs.Length > 0 ? Mathf.Abs(inputs[0]) : 0f;
            float rightInput = inputs != null && inputs.Length > 1 ? Mathf.Abs(inputs[1]) : 0f;
            float leftAngle = leftInput * 90f;
            float rightAngle = rightInput * 90f;

            SetText(inputLeftText, $"Input esquerdo: {leftInput * 100f:0}% | {leftAngle:0} graus");
            SetText(inputRightText, $"Input direito: {rightInput * 100f:0}% | {rightAngle:0} graus");
        }

        if (status != null)
        {
            SetSliderText(camRotationText, "Rotacao da camera", status.camRotation, false, "0.0", " graus");
            SetSliderText(camHorizontalText, "Camera horizontal", status.camHorizontalMove, true, "0", "");
            SetSliderText(camVerticalText, "Camera vertical", status.camVerticalMove, true, "0", "");
            SetSliderText(camZoomText, "Camera zoom", status.camZoom, true, "0", "");
        }
    }

    private void SetSliderText(TMP_Text label, string prefix, Slider slider, bool normalizedPercent, string format, string suffix)
    {
        if (label == null || slider == null)
            return;

        float value = normalizedPercent ? slider.normalizedValue * 100f : slider.value;
        string percent = normalizedPercent ? "%" : "";
        label.text = $"{prefix}: {value.ToString(format)}{percent}{suffix}";
    }

    private void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }

    private void ResetReadouts()
    {
        SetText(inputLeftText, "Input esquerdo:");
        SetText(inputRightText, "Input direito:");
        SetText(camRotationText, "Rotacao da camera:");
        SetText(camHorizontalText, "Camera horizontal:");
        SetText(camVerticalText, "Camera vertical:");
        SetText(camZoomText, "Camera zoom:");
    }

    private void ShowScreenshotToast(string message)
    {
        EnsureScreenshotToast();
        screenshotToastText.text = message;
        screenshotToastCanvas.gameObject.SetActive(true);

        if (screenshotToastCoroutine != null)
            StopCoroutine(screenshotToastCoroutine);
        screenshotToastCoroutine = StartCoroutine(HideScreenshotToastAfterDelay());
    }

    private IEnumerator HideScreenshotToastAfterDelay()
    {
        yield return new WaitForSeconds(2.2f);
        if (screenshotToastCanvas != null)
            screenshotToastCanvas.gameObject.SetActive(false);
    }

    private void EnsureScreenshotToast()
    {
        if (screenshotToastCanvas != null)
            return;

        var canvasObject = new GameObject("Screenshot Saved Toast");
        canvasObject.transform.SetParent(transform, false);

        screenshotToastCanvas = canvasObject.AddComponent<Canvas>();
        screenshotToastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenshotToastCanvas.sortingOrder = 300;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Toast Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 36f);
        panelRect.sizeDelta = new Vector2(520f, 48f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);
        panelImage.raycastTarget = false;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 6f);
        textRect.offsetMax = new Vector2(-16f, -6f);

        screenshotToastText = textObject.AddComponent<TextMeshProUGUI>();
        screenshotToastText.alignment = TextAlignmentOptions.Center;
        screenshotToastText.fontSize = 20f;
        screenshotToastText.fontStyle = FontStyles.Bold;
        screenshotToastText.color = Color.white;
        screenshotToastText.raycastTarget = false;

        screenshotToastCanvas.gameObject.SetActive(false);
    }
}
