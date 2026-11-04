using UnityEngine;

// Esse script serve para mudar a cor do personagem
public class ChangeSkinColor : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material _handMaterial;
    [SerializeField] private Material _legMaterial;
    [SerializeField] private Material _genericMaterial;

    public void ChangeHandSkin(Texture handTexture)
    {
        _handMaterial.mainTexture = handTexture;
    }

    public void ChangeLegSkin(Texture legTexture)
    {
        _legMaterial.mainTexture = legTexture;
        _genericMaterial.mainTexture = legTexture;
    }
}
