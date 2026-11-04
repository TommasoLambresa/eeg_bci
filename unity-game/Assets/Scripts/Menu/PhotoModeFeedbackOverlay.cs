using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoModeFeedbackOverlay : MonoBehaviour
{
    private enum ScreenSide
    {
        Left,
        Right
    }

    private const float MinAngle = 0f;
    private const float MaxAngle = 90f;
    private const float GaugeTravel = 420f;
    // Ajuste manual das barras: altere estes valores para testar outras posicoes na tela.
    // Valores negativos no lado direito puxam a barra para dentro da tela.
    private static readonly Vector2 LeftGaugePosition = new Vector2(515f, 250f);
    private static readonly Vector2 RightGaugePosition = new Vector2(-515f, 250f);

    private PhotoMode owner;
    private GameObject canvasObject;
    private Canvas canvas;
    private RectTransform visualRoot;
    private RectTransform controlPanel;
    private Button controlToggleButton;
    private TMP_Text controlToggleText;

    private Toggle sideViewLeftToggle;
    private Toggle sideViewRightToggle;
    private Toggle gaugeLeftToggle;
    private Toggle gaugeRightToggle;
    private Toggle arcLeftToggle;
    private Toggle arcRightToggle;

    private Slider leftTargetAngleSlider;
    private Slider leftToleranceSlider;
    private Slider rightTargetAngleSlider;
    private Slider rightToleranceSlider;
    private TMP_Text leftTargetAngleText;
    private TMP_Text leftToleranceText;
    private TMP_Text rightTargetAngleText;
    private TMP_Text rightToleranceText;

    private AngleGauge leftGauge;
    private AngleGauge rightGauge;
    private SideView leftSideView;
    private SideView rightSideView;

    private bool initialized;
    private bool overlaysVisible;
    private bool controlsExpanded;
    private bool controlsAllowed = true;
    private bool leftSideViewUsesRightLeg;
    private bool rightSideViewUsesRightLeg = true;

    public void SetOverlayActive(bool active)
    {
        EnsureInitialized();
        overlaysVisible = active;
        canvas.gameObject.SetActive(active);

        if (!active)
        {
            SetVisualsActive(false);
            SetSideCamerasActive(false);
            return;
        }

        controlsExpanded = false;
        SetControlsVisible(true);
        ApplyLayout();
    }

    public void SetControlsVisible(bool visible)
    {
        EnsureInitialized();
        controlsAllowed = visible;
        controlToggleButton.gameObject.SetActive(visible && overlaysVisible);
        controlPanel.gameObject.SetActive(visible && overlaysVisible && controlsExpanded);
    }

    private void Awake()
    {
        owner = GetComponent<PhotoMode>();
    }

    private void OnDisable()
    {
        if (initialized)
            SetOverlayActive(false);
    }

    private void OnDestroy()
    {
        if (leftSideView != null)
            leftSideView.Dispose();
        if (rightSideView != null)
            rightSideView.Dispose();
        if (canvasObject != null)
            Destroy(canvasObject);
    }

    private void Update()
    {
        if (!overlaysVisible)
            return;

        if (owner != null && !owner.modeActive)
        {
            SetOverlayActive(false);
            return;
        }

        UpdateValues();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        CreateCanvas();
        CreateVisuals();
        CreateControls();

        initialized = true;
        overlaysVisible = false;
        canvas.gameObject.SetActive(false);
        SetVisualsActive(false);
        SetSideCamerasActive(false);
    }

    private void CreateCanvas()
    {
        canvasObject = new GameObject("Photo Feedback Overlay");
        canvasObject.hideFlags = HideFlags.DontSave;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        visualRoot = CreateRect("Feedback Visuals", canvasObject.transform);
        Stretch(visualRoot);
    }

    private void CreateVisuals()
    {
        leftGauge = new AngleGauge(CreateGaugeRoot("Left Progress Gauge", ScreenSide.Left), "E");
        rightGauge = new AngleGauge(CreateGaugeRoot("Right Progress Gauge", ScreenSide.Right), "D");
        leftSideView = new SideView(CreateSideViewRoot("Left Side View", ScreenSide.Left), ScreenSide.Left);
        rightSideView = new SideView(CreateSideViewRoot("Right Side View", ScreenSide.Right), ScreenSide.Right);
    }

    private RectTransform CreateGaugeRoot(string name, ScreenSide side)
    {
        var root = CreatePanel(name, visualRoot, Color.clear);
        Anchor(root,
            side == ScreenSide.Left ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
            side == ScreenSide.Left ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
            side == ScreenSide.Left ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
            side == ScreenSide.Left ? LeftGaugePosition : RightGaugePosition,
            new Vector2(190f, 560f));
        return root;
    }

    private RectTransform CreateSideViewRoot(string name, ScreenSide side)
    {
        var root = CreatePanel(name, visualRoot, Color.black);
        Anchor(root,
            side == ScreenSide.Left ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            side == ScreenSide.Left ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            side == ScreenSide.Left ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            side == ScreenSide.Left ? new Vector2(62f, -118f) : new Vector2(-62f, -118f),
            new Vector2(440f, 292f));
        return root;
    }

    private void CreateControls()
    {
        var toggleRect = CreatePanel("Feedback Toggle", canvas.transform, new Color(0f, 0f, 0f, 0.62f));
        Anchor(toggleRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(148f, 38f));
        toggleRect.GetComponent<Image>().raycastTarget = true;
        controlToggleButton = toggleRect.gameObject.AddComponent<Button>();
        controlToggleButton.targetGraphic = toggleRect.GetComponent<Image>();
        controlToggleButton.onClick.AddListener(ToggleControls);
        controlToggleText = CreateText(toggleRect, "Feedback", 16, FontStyles.Bold);
        controlToggleText.alignment = TextAlignmentOptions.Center;
        Stretch(controlToggleText.rectTransform);

        controlPanel = CreatePanel("Photo Feedback Controls", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
        Anchor(controlPanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 64f), new Vector2(560f, 690f));
        controlPanel.GetComponent<Image>().raycastTarget = true;

        var scrollRectTransform = CreateRect("Scroll View", controlPanel);
        Stretch(scrollRectTransform, 10f);
        var scroll = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreatePanel("Viewport", scrollRectTransform, new Color(1f, 1f, 1f, 0.04f));
        viewport.GetComponent<Image>().raycastTarget = true;
        Stretch(viewport);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1100f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 20, 20);
        layout.spacing = 16f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        CreateText(content, "Feedback visual", 24, FontStyles.Bold, 36f);
        CreateText(content, "Configure o que aparece no print.", 15, FontStyles.Normal);

        CreateSectionLabel(content, "Vista lateral na vista frontal");
        sideViewLeftToggle = CreateToggle(content, "Esquerda", false);
        sideViewRightToggle = CreateToggle(content, "Direita", true);

        CreateSectionLabel(content, "Barra 0-90");
        gaugeLeftToggle = CreateToggle(content, "Esquerda", false);
        gaugeRightToggle = CreateToggle(content, "Direita", true);

        CreateSectionLabel(content, "Alvo na vista lateral");
        arcLeftToggle = CreateToggle(content, "Esquerda", false);
        arcRightToggle = CreateToggle(content, "Direita", true);

        CreateSectionLabel(content, "Alvo esquerdo");
        leftTargetAngleText = CreateText(content, "Alvo esquerdo: 30 graus", 18, FontStyles.Bold, 30f);
        leftTargetAngleSlider = CreateSlider(content, 0f, 90f, 30f);
        leftToleranceText = CreateText(content, "Tolerancia esquerda: +/- 5 graus", 18, FontStyles.Bold, 30f);
        leftToleranceSlider = CreateSlider(content, 0f, 30f, 5f);

        CreateSectionLabel(content, "Alvo direito");
        rightTargetAngleText = CreateText(content, "Alvo direito: 30 graus", 18, FontStyles.Bold, 30f);
        rightTargetAngleSlider = CreateSlider(content, 0f, 90f, 30f);
        rightToleranceText = CreateText(content, "Tolerancia direita: +/- 5 graus", 18, FontStyles.Bold, 30f);
        rightToleranceSlider = CreateSlider(content, 0f, 30f, 5f);

        sideViewLeftToggle.onValueChanged.AddListener(_ => ApplyLayout());
        sideViewRightToggle.onValueChanged.AddListener(_ => ApplyLayout());
        gaugeLeftToggle.onValueChanged.AddListener(_ => ApplyLayout());
        gaugeRightToggle.onValueChanged.AddListener(_ => ApplyLayout());
        arcLeftToggle.onValueChanged.AddListener(_ => ApplyLayout());
        arcRightToggle.onValueChanged.AddListener(_ => ApplyLayout());
        leftTargetAngleSlider.onValueChanged.AddListener(_ => UpdateValues());
        leftToleranceSlider.onValueChanged.AddListener(_ => UpdateValues());
        rightTargetAngleSlider.onValueChanged.AddListener(_ => UpdateValues());
        rightToleranceSlider.onValueChanged.AddListener(_ => UpdateValues());

        controlPanel.gameObject.SetActive(false);
    }

    private void ToggleControls()
    {
        controlsExpanded = !controlsExpanded;
        controlPanel.gameObject.SetActive(controlsAllowed && overlaysVisible && controlsExpanded);
        if (controlToggleText != null)
            controlToggleText.text = controlsExpanded ? "Ocultar feedback" : "Feedback";
    }

    private void ApplyLayout()
    {
        if (!initialized || !overlaysVisible)
            return;

        bool wantLeftGauge = gaugeLeftToggle.isOn;
        bool wantRightGauge = gaugeRightToggle.isOn;
        bool wantLeftView = sideViewLeftToggle.isOn;
        bool wantRightView = sideViewRightToggle.isOn;

        bool showLeftGauge = wantLeftGauge;
        bool showRightGauge = wantRightGauge;
        bool showLeftView = wantRightView;
        bool showRightView = wantLeftView;
        leftSideViewUsesRightLeg = true;
        rightSideViewUsesRightLeg = false;

        if (wantLeftGauge && wantRightGauge)
        {
            showLeftView = false;
            showRightView = false;
        }
        else if (wantLeftView && wantRightView && (wantLeftGauge || wantRightGauge))
        {
            showLeftGauge = false;
            showRightGauge = false;
        }
        else
        {
            if (wantLeftGauge && showLeftView)
            {
                showLeftView = false;
                if (!wantLeftView)
                {
                    showRightView = true;
                    rightSideViewUsesRightLeg = true;
                }
            }

            if (wantRightGauge && showRightView)
            {
                showRightView = false;
                if (!wantRightView)
                {
                    showLeftView = true;
                    leftSideViewUsesRightLeg = false;
                }
            }
        }

        leftGauge.SetVisible(showLeftGauge);
        rightGauge.SetVisible(showRightGauge);
        leftSideView.SetVisible(showLeftView);
        rightSideView.SetVisible(showRightView);

        UpdateValues();
    }

    private void UpdateValues()
    {
        if (!initialized || !overlaysVisible)
            return;

        float leftTargetAngle = leftTargetAngleSlider != null ? leftTargetAngleSlider.value : 30f;
        float leftTolerance = leftToleranceSlider != null ? leftToleranceSlider.value : 5f;
        float rightTargetAngle = rightTargetAngleSlider != null ? rightTargetAngleSlider.value : 30f;
        float rightTolerance = rightToleranceSlider != null ? rightToleranceSlider.value : 5f;
        Vector2 angles = GetCommandedAngles();

        if (leftTargetAngleText != null)
            leftTargetAngleText.text = $"Alvo esquerdo: {leftTargetAngle:0} graus";
        if (leftToleranceText != null)
            leftToleranceText.text = $"Tolerancia esquerda: +/- {leftTolerance:0} graus";
        if (rightTargetAngleText != null)
            rightTargetAngleText.text = $"Alvo direito: {rightTargetAngle:0} graus";
        if (rightToleranceText != null)
            rightToleranceText.text = $"Tolerancia direita: +/- {rightTolerance:0} graus";

        leftGauge.UpdateValues(angles.x, leftTargetAngle, leftTolerance);
        rightGauge.UpdateValues(angles.y, rightTargetAngle, rightTolerance);
        leftSideView.UpdateCamera(
            leftSideViewUsesRightLeg ? angles.y : angles.x,
            leftSideViewUsesRightLeg ? rightTargetAngle : leftTargetAngle,
            leftSideViewUsesRightLeg ? rightTolerance : leftTolerance,
            leftSideViewUsesRightLeg ? arcRightToggle != null && arcRightToggle.isOn : arcLeftToggle != null && arcLeftToggle.isOn,
            leftSideViewUsesRightLeg);
        rightSideView.UpdateCamera(
            rightSideViewUsesRightLeg ? angles.y : angles.x,
            rightSideViewUsesRightLeg ? rightTargetAngle : leftTargetAngle,
            rightSideViewUsesRightLeg ? rightTolerance : leftTolerance,
            rightSideViewUsesRightLeg ? arcRightToggle != null && arcRightToggle.isOn : arcLeftToggle != null && arcLeftToggle.isOn,
            rightSideViewUsesRightLeg);

        leftSideView.UpdateGhostTarget(rightTargetAngle, arcRightToggle != null && arcRightToggle.isOn, true);
        rightSideView.UpdateGhostTarget(leftTargetAngle, arcLeftToggle != null && arcLeftToggle.isOn, false);
    }

    private Vector2 GetCommandedAngles()
    {
        var player = PlayerController.instance;
        if (player == null)
            return Vector2.zero;

        float[] inputs = player.GetInputValues();
        float left = inputs != null && inputs.Length > 0 ? Mathf.Abs(inputs[0]) * MaxAngle : 0f;
        float right = inputs != null && inputs.Length > 1 ? Mathf.Abs(inputs[1]) * MaxAngle : 0f;
        return new Vector2(Mathf.Clamp(left, MinAngle, MaxAngle), Mathf.Clamp(right, MinAngle, MaxAngle));
    }

    private void SetVisualsActive(bool active)
    {
        if (leftGauge != null) leftGauge.SetVisible(active);
        if (rightGauge != null) rightGauge.SetVisible(active);
        if (leftSideView != null) leftSideView.SetVisible(active);
        if (rightSideView != null) rightSideView.SetVisible(active);
        if (!active)
        {
            if (leftSideView != null) leftSideView.UpdateGhostTarget(0f, false, true);
            if (rightSideView != null) rightSideView.UpdateGhostTarget(0f, false, false);
        }
    }

    private void SetSideCamerasActive(bool active)
    {
        if (leftSideView != null) leftSideView.SetCameraActive(active);
        if (rightSideView != null) rightSideView.SetCameraActive(active);
    }

    private void CreateSectionLabel(Transform parent, string value)
    {
        var label = CreateText(parent, value, 16, FontStyles.Bold, 28f);
        label.color = new Color(0.78f, 0.92f, 1f, 1f);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string value, int size, FontStyles style, float height = 0f)
    {
        var rect = CreateRect("Text", parent);
        rect.sizeDelta = new Vector2(0f, height > 0f ? height : size + 18f);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Toggle CreateToggle(Transform parent, string label, bool defaultValue)
    {
        var root = CreateRect(label, parent);
        root.sizeDelta = new Vector2(0f, 44f);
        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        hitArea.raycastTarget = true;

        var background = CreatePanel("Box", root, new Color(1f, 1f, 1f, 0.95f));
        background.GetComponent<Image>().raycastTarget = true;
        Anchor(background, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(15f, 0f), new Vector2(26f, 26f));

        var check = CreatePanel("Check", background, new Color(0.1f, 0.75f, 0.35f, 1f));
        Stretch(check, 4f);

        var text = CreateText(root, label, 18, FontStyles.Normal, 44f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Anchor(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(-54f, 0f));

        var toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = defaultValue;
        return toggle;
    }

    private static Slider CreateSlider(Transform parent, float min, float max, float value)
    {
        var root = CreateRect("Slider", parent);
        root.sizeDelta = new Vector2(0f, 46f);

        var background = CreatePanel("Background", root, new Color(1f, 1f, 1f, 0.62f));
        Anchor(background, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 6f));
        background.GetComponent<Image>().raycastTarget = true;

        var handleArea = CreateRect("Handle Slide Area", root);
        Stretch(handleArea);
        var handle = CreatePanel("Handle", handleArea, Color.white);
        handle.GetComponent<Image>().raycastTarget = true;
        handle.sizeDelta = new Vector2(22f, 22f);

        var slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.fillRect = null;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private class AngleGauge
    {
        private readonly RectTransform root;
        private readonly RectTransform targetArea;
        private readonly RectTransform currentLine;
        private readonly TMP_Text title;
        private readonly string label;

        public AngleGauge(RectTransform root, string label)
        {
            this.root = root;
            this.label = label;

            var track = CreatePanel("Track", root, new Color(0.32f, 0.36f, 0.36f, 0.78f));
            Anchor(track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 0f), new Vector2(44f, GaugeTravel + 34f));

            var inner = CreatePanel("Inner", track, new Color(1f, 1f, 1f, 0.74f));
            Stretch(inner, 10f);

            targetArea = CreatePanel("Target Area", track, new Color(0.25f, 0.86f, 0.38f, 0.65f));
            currentLine = CreatePanel("Current Angle", track, new Color(1f, 0.07f, 0.04f, 1f));

            title = CreateText(root, $"{label}: 0 graus", 16, FontStyles.Bold);
            title.color = new Color(0f, 0f, 0f, 0.68f);
            title.alignment = TextAlignmentOptions.Center;
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-20f, -6f), new Vector2(0f, 24f));

            for (int angle = 0; angle <= 90; angle += 5)
            {
                bool major = angle % 15 == 0;
                bool labeled = angle == 0 || angle == 30 || angle == 45 || angle == 60 || angle == 90;
                float y = AngleToY(angle);

                var tick = CreatePanel("Tick", root, new Color(0.18f, 0.18f, 0.18f, 0.72f));
                Anchor(tick, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, y), new Vector2(major ? 34f : 18f, major ? 4f : 2f));

                if (labeled)
                {
                    var text = CreateText(root, angle.ToString(), 19, FontStyles.Bold);
                    text.color = new Color(0.18f, 0.18f, 0.18f, 0.72f);
                    Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, y), new Vector2(46f, 28f));
                }
            }
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
        }

        public void UpdateValues(float currentAngle, float targetAngle, float tolerance)
        {
            if (!root.gameObject.activeSelf)
                return;

            currentAngle = Mathf.Clamp(currentAngle, MinAngle, MaxAngle);
            float targetMin = Mathf.Clamp(targetAngle - tolerance, MinAngle, MaxAngle);
            float targetMax = Mathf.Clamp(targetAngle + tolerance, MinAngle, MaxAngle);

            float yMin = AngleToY(targetMin);
            float yMax = AngleToY(targetMax);
            float yCurrent = AngleToY(currentAngle);

            Anchor(targetArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, (yMin + yMax) * 0.5f), new Vector2(76f, Mathf.Max(8f, yMax - yMin)));
            Anchor(currentLine, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, yCurrent), new Vector2(88f, 5f));
            title.text = $"{label}: {currentAngle:0} graus";
        }

        private static float AngleToY(float angle)
        {
            return Mathf.Lerp(-GaugeTravel * 0.5f, GaugeTravel * 0.5f, Mathf.InverseLerp(MinAngle, MaxAngle, angle));
        }
    }

    private class SideView
    {
        private readonly RectTransform root;
        private readonly Camera camera;
        private readonly RenderTexture renderTexture;
        private readonly ScreenSide side;
        private readonly PhotoModeArcGraphic arc;
        private readonly PhotoModeArcGraphic targetArc;
        private readonly GhostLegTarget ghostLegTarget;

        public SideView(RectTransform root, ScreenSide side)
        {
            this.root = root;
            this.side = side;

            var border = root.GetComponent<Image>();
            border.raycastTarget = false;

            var imageObject = new GameObject("Image");
            imageObject.transform.SetParent(root, false);
            var imageRect = imageObject.AddComponent<RectTransform>();
            Stretch(imageRect, 6f);
            var image = imageObject.AddComponent<RawImage>();
            image.raycastTarget = false;

            renderTexture = new RenderTexture(840, 540, 16);
            image.texture = renderTexture;

            var cameraObject = new GameObject(root.name + " Camera");
            cameraObject.hideFlags = HideFlags.DontSave;
            camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = renderTexture;
            camera.enabled = false;

            var targetArcRoot = CreateRect("Target Arc", root);
            Anchor(targetArcRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(70f, 76f), new Vector2(126f, 126f));
            targetArc = targetArcRoot.gameObject.AddComponent<PhotoModeArcGraphic>();
            targetArc.color = new Color(0.25f, 0.9f, 0.35f, 0.32f);
            targetArc.radius = 54f;
            targetArc.thickness = 16f;
            targetArc.raycastTarget = false;
            targetArc.gameObject.SetActive(false);

            ghostLegTarget = new GhostLegTarget(side);

            var arcRoot = CreateRect("Current Arc", root);
            Anchor(arcRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(70f, 76f), new Vector2(115f, 115f));
            arc = arcRoot.gameObject.AddComponent<PhotoModeArcGraphic>();
            arc.color = new Color(1f, 0.07f, 0.04f, 0.9f);
            arc.radius = 49f;
            arc.thickness = 5f;
            arc.raycastTarget = false;
            arc.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            SetCameraActive(visible);
        }

        public void SetCameraActive(bool active)
        {
            if (camera != null)
                camera.enabled = active;
        }

        public void UpdateCamera(float angle, float targetAngle, float tolerance, bool showArc, bool useRightLeg)
        {
            if (!root.gameObject.activeSelf || camera == null)
                return;

            PositionCamera();

            arc.gameObject.SetActive(showArc);
            targetArc.gameObject.SetActive(showArc);

            if (showArc)
            {
                float minTarget = Mathf.Clamp(targetAngle - tolerance, MinAngle, MaxAngle);
                float maxTarget = Mathf.Clamp(targetAngle + tolerance, MinAngle, MaxAngle);
                targetArc.SetRange(minTarget, maxTarget);
            }

            arc.SetAngle(angle);
        }

        public void UpdateGhostTarget(float targetAngle, bool visible, bool useRightLeg)
        {
            ghostLegTarget.SetVisible(visible, useRightLeg);
            if (visible)
                ghostLegTarget.UpdateTarget(targetAngle, useRightLeg);
        }

        private void PositionCamera()
        {
            var player = PlayerController.instance;
            if (player == null)
                return;

            Camera source = player.lateralCam != null ? player.lateralCam : player.mainCam;
            if (source != null)
            {
                camera.CopyFrom(source);
                camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                camera.fieldOfView = source.fieldOfView;
                if (side == ScreenSide.Right)
                    camera.transform.RotateAround(player.transform.position, Vector3.up, 180f);
            }

            camera.targetTexture = renderTexture;
            camera.enabled = true;
        }

        public void Dispose()
        {
            if (camera != null)
                Object.Destroy(camera.gameObject);
            if (renderTexture != null)
                renderTexture.Release();
            if (ghostLegTarget != null)
                ghostLegTarget.Dispose();
        }
    }

    private class GhostLegTarget
    {
        private ScreenSide side;
        private GameObject cloneRoot;
        private RaiseLeg[] cloneLegControllers;
        private Material ghostMaterial;
        private Transform[] originalTransforms;
        private Transform[] cloneTransforms;

        public GhostLegTarget(ScreenSide side)
        {
            this.side = side;
        }

        public void SetVisible(bool visible, bool useRightLeg)
        {
            if (visible)
                EnsureClone(useRightLeg ? ScreenSide.Right : ScreenSide.Left);

            if (cloneRoot != null)
                cloneRoot.SetActive(visible);
        }

        public void UpdateTarget(float targetAngle, bool useRightLeg)
        {
            EnsureClone(useRightLeg ? ScreenSide.Right : ScreenSide.Left);
            if (cloneRoot == null)
                return;

            cloneRoot.SetActive(true);
            SyncTransform();
            ApplyTargetPose(targetAngle);
        }

        public void Dispose()
        {
            if (cloneRoot != null)
                Object.Destroy(cloneRoot);
            if (ghostMaterial != null)
                Object.Destroy(ghostMaterial);
            originalTransforms = null;
            cloneTransforms = null;
        }

        private void EnsureClone(ScreenSide requestedSide)
        {
            if (cloneRoot != null && side == requestedSide)
                return;

            Dispose();
            side = requestedSide;

            var player = PlayerController.instance;
            if (player == null)
                return;

            cloneRoot = Object.Instantiate(player.gameObject, player.transform.position, player.transform.rotation);
            cloneRoot.name = side == ScreenSide.Left ? "Ghost Target Leg Left" : "Ghost Target Leg Right";
            cloneRoot.hideFlags = HideFlags.DontSave;
            BuildTransformPairs(player.transform);

            RemoveRuntimeComponents();
            HideNonTargetRenderers(player);
            ApplyGhostMaterial();
            SyncTransform();

            cloneLegControllers = cloneRoot.GetComponentsInChildren<RaiseLeg>(true);
            cloneRoot.SetActive(false);
        }

        private void RemoveRuntimeComponents()
        {
            foreach (var behaviour in cloneRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is RaiseLeg)
                    continue;
                Object.Destroy(behaviour);
            }

            foreach (var joint in cloneRoot.GetComponentsInChildren<Joint>(true))
                Object.Destroy(joint);
            foreach (var rb in cloneRoot.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rb);
            foreach (var collider in cloneRoot.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
            foreach (var cameraComponent in cloneRoot.GetComponentsInChildren<Camera>(true))
                Object.Destroy(cameraComponent);
            foreach (var listener in cloneRoot.GetComponentsInChildren<AudioListener>(true))
                Object.Destroy(listener);
        }

        private void HideNonTargetRenderers(PlayerController player)
        {
            foreach (var renderer in cloneRoot.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            int index = side == ScreenSide.Left ? 1 : 0;
            ShowCloneRenderer(player._pernaNormal, index);
            ShowCloneRenderer(player._pernaProtese, index);
            ShowCloneRenderer(player._coxaProtese, index);
            ShowCloneRenderer(player._peProtese, index);
        }

        private void ShowCloneRenderer(GameObject[] originals, int index)
        {
            if (originals == null || index < 0 || index >= originals.Length || originals[index] == null)
                return;

            string path = GetPath(PlayerController.instance.transform, originals[index].transform);
            Transform cloneTransform = cloneRoot.transform.Find(path);
            if (cloneTransform == null)
                return;

            foreach (var renderer in cloneTransform.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        private void ApplyGhostMaterial()
        {
            ApplyTransparentOriginalMaterials();
        }

        private void ApplyTransparentOriginalMaterials()
        {
            foreach (var renderer in cloneRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                    continue;

                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                    ConfigureOriginalTransparentMaterial(materials[i], new Color(0.78f, 0.9f, 1f, 0.32f));
                renderer.materials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void ConfigureOriginalTransparentMaterial(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void ConfigureTransparentMaterial(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void SyncTransform()
        {
            var player = PlayerController.instance;
            if (player == null || cloneRoot == null)
                return;

            cloneRoot.transform.SetPositionAndRotation(player.transform.position, player.transform.rotation);
            cloneRoot.transform.localScale = player.transform.localScale;

            if (originalTransforms == null || cloneTransforms == null)
                return;

            for (int i = 0; i < originalTransforms.Length && i < cloneTransforms.Length; i++)
            {
                if (originalTransforms[i] == null || cloneTransforms[i] == null)
                    continue;

                cloneTransforms[i].localPosition = originalTransforms[i].localPosition;
                cloneTransforms[i].localRotation = originalTransforms[i].localRotation;
                cloneTransforms[i].localScale = originalTransforms[i].localScale;
            }
        }

        private void ApplyTargetPose(float targetAngle)
        {
            if (cloneLegControllers == null)
                return;

            float normalizedTarget = Mathf.Clamp01(targetAngle / MaxAngle);
            foreach (var leg in cloneLegControllers)
            {
                if (leg == null || leg._bone == null)
                    continue;

                float boneAngle = Mathf.Lerp(leg.minAngle, leg.maxAngle, normalizedTarget);
                Vector3 localEuler = leg._bone.localEulerAngles;
                localEuler.x = boneAngle;
                leg._bone.localEulerAngles = localEuler;
                leg.enabled = false;
            }
        }

        private void BuildTransformPairs(Transform originalRoot)
        {
            var originalList = new System.Collections.Generic.List<Transform>();
            var cloneList = new System.Collections.Generic.List<Transform>();
            Transform[] sourceTransforms = originalRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform sourceTransform in sourceTransforms)
            {
                string path = GetPath(originalRoot, sourceTransform);
                Transform cloneTransform = string.IsNullOrEmpty(path) ? cloneRoot.transform : cloneRoot.transform.Find(path);
                if (cloneTransform == null)
                    continue;

                originalList.Add(sourceTransform);
                cloneList.Add(cloneTransform);
            }

            originalTransforms = originalList.ToArray();
            cloneTransforms = cloneList.ToArray();
        }

        private static string GetPath(Transform root, Transform child)
        {
            if (root == null || child == null)
                return string.Empty;

            string path = child.name;
            Transform current = child.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}
