using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

// Script responsável pela mudança da imagem do momento do Cue
public class CueConfigs : MonoBehaviour
{
    public TMP_InputField field;
    public Button minusBtn,
        imageBtn;
    public string path;
    public TMP_Dropdown dropdown;

    public void SetCueImage()
    {
        var _raw = GrazController.instance.cueImageDirection;

        if (string.IsNullOrEmpty(path))
        {
            // Se o operador não definir nenhum caminho para a imagem, ela automaticamente seta a imagem padrão.
            _raw.texture = GrazController.instance.defaultCueImage;
        }
        else
        {
            // Basicamente aqui serve para converter o arquivo de imagem em textura para a imagem no projeto.
            byte[] fileDate = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileDate);

            _raw.texture = texture;
        }
    }
}
