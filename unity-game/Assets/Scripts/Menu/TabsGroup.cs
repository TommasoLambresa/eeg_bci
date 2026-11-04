using System.Collections.Generic;
using UnityEngine;

// Script que organiza o funcionamento das tabs
public class TabsGroup : MonoBehaviour
{
    public List<TabButtons> _tabs; // Lista de botões de tab

    // Cores para diferentes estados da tab
    public Color _idleColor,
        _hoverColor,
        _activeColor;

    private TabButtons _selectedBtn; // Botão atualmente selecionado

    [SerializeField] private List<GameObject> _objectsView; // Objetos associados a cada tab

    // Adiciona uma tab ao grupo
    public void Subscribe(TabButtons tab)
    {
        if (_tabs == null)
            _tabs = new List<TabButtons>();

        _tabs.Add(tab);

        // Seleciona automaticamente a primeira tab
        if (int.Parse(tab.transform.name.Substring(tab.name.Length - 1)) - 1 == 0)
            OnTabSelect(tab);
        else
            OnTabExit(tab);
    }

    // Quando mouse entra em uma tab
    public void OnTabEnter(TabButtons tab)
    {
        ResetTabs();
        if (_selectedBtn == null || tab != _selectedBtn)
            tab._background.color = _hoverColor;
    }

    // Quando mouse sai de uma tab
    public void OnTabExit(TabButtons tab)
    {
        ResetTabs();
    }

    // Quando uma tab é selecionada
    public void OnTabSelect(TabButtons tab)
    {
        if (_selectedBtn != null)
            _selectedBtn.Deselect();

        _selectedBtn = tab;
        _selectedBtn.Select();

        ResetTabs();
        tab._background.color = _activeColor;

        // Ativa/desativa objetos baseado na tab selecionada
        int index = int.Parse(tab.transform.name.Substring(tab.name.Length - 1));
        for (int i = 0; i < _objectsView.Count; i++)
        {
            if (i == index - 1)
                _objectsView[i].SetActive(true);
            else
                _objectsView[i].SetActive(false);
        }
    }

    // Reseta todas as tabs para estado normal
    public void ResetTabs()
    {
        foreach (var btn in _tabs)
        {
            if (_selectedBtn != null && btn == _selectedBtn) continue;
            btn._background.color = _idleColor;
        }
    }
}
