using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuBtnOperation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDeselectHandler
{
    [SerializeField] private UnityEvent _myEvent;

    [Space, Header("Outline")]
    public Image _outlineImage; // Imagem de outline

    [Space, Header("Handler")]
    public Color _colorHandler; // Cor ao passar mouse
    public Color _colorNormal; // Cor normal
    public Color _colorClick; // Cor clicar

    [HideInInspector] public bool isSelected; // Se está selecionado

    [SerializeField] private bool OnSkin; // Flag que identifica se o botão serve para escolher a cor do personagem

    private void Start()
    {
        _outlineImage.color = _colorNormal;
    }

    // Quando desselecionar
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        _outlineImage.color = _colorNormal;
    }

    // Quando clicar
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Comportamento para Toggle
        if (transform.parent.TryGetComponent(out Toggle _toggle))
        {
            StartCoroutine(ClickToggleColor());
            _myEvent.Invoke();
            return;
        }

        // Comportamento para Cor
        if (OnSkin && UIConfigs.instance.actSkin != this)
        {
            if (UIConfigs.instance.actSkin != null)
            {
                UIConfigs.instance.actSkin.isSelected = false;
                UIConfigs.instance.actSkin._outlineImage.color = UIConfigs.instance.actSkin._colorNormal;
            }
            UIConfigs.instance.actSkin = this;
        }

        isSelected = true;
        StartCoroutine(ClickColor());
        _myEvent.Invoke();
    }

    // Quando o mouse entrar
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected) return;

        _outlineImage.color = _colorHandler;
    }

    // Quando o mouse sair
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;

        _outlineImage.color = _colorNormal;
    }

    // Alterna estado do Toggle
    public void Toggled(Toggle toggle)
    {
        toggle.isOn = !toggle.isOn;
    }

    // Efeito visual de clique
    private IEnumerator ClickColor()
    {
        _outlineImage.color = _colorHandler;
        yield return new WaitForSeconds(.1f);
        _outlineImage.color = _colorClick;
    }

    // Efeito visual de clique em Toggle
    private IEnumerator ClickToggleColor()
    {
        _outlineImage.color = _colorHandler;
        yield return new WaitForSeconds(.1f);
        _outlineImage.color = _colorClick;
        yield return new WaitForSeconds(.1f);
        _outlineImage.color = _colorNormal;
    }


}
