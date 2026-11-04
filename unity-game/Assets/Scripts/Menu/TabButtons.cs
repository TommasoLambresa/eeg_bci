using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(Image))]
public class TabButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TabsGroup _group; // Referencia ao grupo de tabs

    [HideInInspector] public Image _background; // Imagem de fundo do botão

    public UnityEvent onTabSelected; // Evento quando tab é selecionada
    public UnityEvent onTabDeselected; // Evento quando tab é desselecionada

    private void Start()
    {
        _background = GetComponent<Image>();
        _group = GetComponentInParent<TabsGroup>();
        _group.Subscribe(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _group.OnTabEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _group.OnTabExit(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _group.OnTabSelect(this);
    }

    public void Select()
    {
        onTabSelected?.Invoke();
    }

    public void Deselect()
    {
        onTabDeselected?.Invoke();
    }
}