using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback visual contínuo do predict_proba.
///
/// Importante: este componente NÃO controla o avatar. Ele apenas apresenta a
/// evidência do classificador. O movimento continua vindo de GrazMI_Control.
/// </summary>
public sealed class ClassifierProbabilityView : MonoBehaviour
{
    public GameObject root;

    [Header("Barras")]
    public Image leftFill;
    public Image restFill;
    public Image rightFill;

    public TMP_Text leftValueText;
    public TMP_Text restValueText;
    public TMP_Text rightValueText;

    public CanvasGroup leftGroup;
    public CanvasGroup restGroup;
    public CanvasGroup rightGroup;

    [Header("Estado")]
    public TMP_Text statusText;

    [Header("Visual")]
    [Tooltip("Suavização apenas visual das barras. Não altera o classificador nem o controle motor.")]
    [Min(0f)] public float visualSmoothing = 12f;

    private float targetLeft;
    private float targetRest;
    private float targetRight;

    private float shownLeft;
    private float shownRest;
    private float shownRight;

    private bool activeLeft = true;
    private bool activeRest = true;
    private bool activeRight = true;

    private int predictedClassId = -1;
    private float classifierConfidence;
    private int controlStateId;
    private float controlConfidence;

    public void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
    }

    public void ResetView()
    {
        targetLeft = targetRest = targetRight = 0f;
        shownLeft = shownRest = shownRight = 0f;
        activeLeft = activeRest = activeRight = true;
        predictedClassId = -1;
        classifierConfidence = 0f;
        controlStateId = 0;
        controlConfidence = 0f;
        RefreshImmediate();
    }

    public void PushProbabilities(
        float pLeft,
        float pRest,
        float pRight,
        bool isLeftActive,
        bool isRestActive,
        bool isRightActive,
        int classId,
        float confidence)
    {
        targetLeft = Mathf.Clamp01(pLeft);
        targetRest = Mathf.Clamp01(pRest);
        targetRight = Mathf.Clamp01(pRight);

        activeLeft = isLeftActive;
        activeRest = isRestActive;
        activeRight = isRightActive;

        predictedClassId = classId;
        classifierConfidence = Mathf.Clamp01(confidence);
    }

    public void SetControlState(int stateId, float confidence)
    {
        controlStateId = stateId;
        controlConfidence = Mathf.Clamp01(confidence);
    }

    private void Update()
    {
        float k = visualSmoothing <= 0f
            ? 1f
            : 1f - Mathf.Exp(-visualSmoothing * Time.unscaledDeltaTime);

        shownLeft = Mathf.Lerp(shownLeft, targetLeft, k);
        shownRest = Mathf.Lerp(shownRest, targetRest, k);
        shownRight = Mathf.Lerp(shownRight, targetRight, k);

        RefreshImmediate();
    }

    private void RefreshImmediate()
    {
        SetBar(leftFill, leftValueText, leftGroup, shownLeft, activeLeft);
        SetBar(restFill, restValueText, restGroup, shownRest, activeRest);
        SetBar(rightFill, rightValueText, rightGroup, shownRight, activeRight);

        if (statusText != null)
        {
            string classifier = ClassLabel(predictedClassId);
            string control = ControlLabel(controlStateId);

            string classifierPart = predictedClassId < 0
                ? "CLASSIF.: --"
                : $"CLASSIF.: {classifier}  {classifierConfidence * 100f:0}%";

            statusText.text = $"{classifierPart}    |    CONTROLE: {control}";
        }
    }

    private static void SetBar(
        Image fill,
        TMP_Text valueText,
        CanvasGroup group,
        float value,
        bool active)
    {
        if (group != null)
            group.alpha = active ? 1f : 0.28f;

        if (fill != null)
            fill.fillAmount = active ? Mathf.Clamp01(value) : 0f;

        if (valueText != null)
            valueText.text = active ? $"{Mathf.Clamp01(value) * 100f:0}%" : "—";
    }

    private static string ClassLabel(int id)
    {
        switch (id)
        {
            case 0: return "REST";
            case 1: return "ESQUERDA";
            case 2: return "DIREITA";
            default: return "--";
        }
    }

    private static string ControlLabel(int id)
    {
        switch (id)
        {
            case 1: return "ESQUERDA";
            case 2: return "BOTH (LEGADO)";
            case 3: return "DIREITA";
            default: return "REST";
        }
    }
}
