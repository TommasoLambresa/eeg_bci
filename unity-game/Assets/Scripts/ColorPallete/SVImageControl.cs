using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Controla e atualiza o SV do seletor de cor
public class SVImageControl : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    [SerializeField] private Image selectImage;

    private RawImage svImage;
    private ColorChoiceController colorChoice;
    private RectTransform rectTransform,
        selectTransform;

    public void OnDrag(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }

    private void Awake()
    {
        svImage = GetComponent<RawImage>();
        colorChoice = FindAnyObjectByType<ColorChoiceController>();
        rectTransform = GetComponent<RectTransform>();

        selectTransform = selectImage.GetComponent<RectTransform>();
        selectTransform.position = new Vector2(-(rectTransform.sizeDelta.x * .5f), -(rectTransform.sizeDelta.y * .5f));
    }

    // Atualiza cor baseada na posição do cursor
    private void UpdateColor(PointerEventData eventData)
    {
        // Converte posição do cursor para coordenadas locais
        Vector3 pos = rectTransform.InverseTransformPoint(eventData.position);

        // Calcula limites do retangulo
        float deltaX = rectTransform.sizeDelta.x * .5f;
        float deltaY = rectTransform.sizeDelta.y * .5f;

        // Limita posição dentro dos limites
        if (pos.x < -deltaX)
            pos.x = -deltaX;
        else if (pos.x > deltaX)
            pos.x = deltaX;

        if (pos.y < -deltaY)
            pos.y = -deltaY;
        else if (pos.y > deltaY)
            pos.y = deltaY;

        // Converte para coordenadas normalizadas
        float x = pos.x + deltaX;
        float y = pos.y + deltaY;

        float xNorm = x / rectTransform.sizeDelta.x; // Saturação
        float yNorm = y / rectTransform.sizeDelta.y; // Valor

        // Atualiza posição do seletor visual
        selectTransform.localPosition = pos;
        // Inverte cor do seletor para contraste
        selectImage.color = Color.HSVToRGB(0, 0, 1 - yNorm);

        // Atualiza valores no controlador principal
        colorChoice.SetSV(xNorm, yNorm);
    }
}
