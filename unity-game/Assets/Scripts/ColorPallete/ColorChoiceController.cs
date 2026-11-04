using UnityEngine;
using UnityEngine.UI;
using TMPro;

// O script cria a UI de seleção de cor
public class ColorChoiceController : MonoBehaviour
{
    // Valores HSV atuais
    public float currentHue,
        currentSat,
        currentVal;

    [SerializeField] private RawImage hueImage,
        satValImage,
        outputImage;

    [SerializeField] private Slider hueSlider; // Slider para controle de matiz

    [SerializeField] private TMP_InputField hexInputField; // Campo de input hexadecimal

    private Texture2D hueTexture,
        svTexture,
        outputTexture;

    [SerializeField] private string defaultHex; // Cor hexadecimal padrão
    [SerializeField] private Image currentImage; // Imagem que mostra a cor atual

    private void Start()
    {
        CreateHueImage();
        CreateSVImage();
        CreateOutputImage();
        UpdateOutputImage();

        hexInputField.text = defaultHex;
        OnTextInput();
    }

    // Cria textura do seletor de matiz
    private void CreateHueImage()
    {
        hueTexture = new Texture2D(1, 16);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        hueTexture.name = "HueTexture";

        for (int i = 0; i < hueTexture.height; i++)
        {
            hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture.height, 1, 1f));
        }

        hueTexture.Apply();
        currentHue = 0;

        hueImage.texture = hueTexture;
    }

    // Cria textura do seletor de saturação/valor
    private void CreateSVImage()
    {
        svTexture = new Texture2D(16, 16);
        svTexture.wrapMode = TextureWrapMode.Clamp;
        svTexture.name = "SatValTexture";

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y / svTexture.height));
            }
        }

        svTexture.Apply();
        currentSat = 0;
        currentVal = 0;

        satValImage.texture = svTexture;
    }

    // Cria textura de saída com a cor atual
    private void CreateOutputImage()
    {
        outputTexture = new Texture2D(1, 16);
        outputTexture.wrapMode = TextureWrapMode.Clamp;
        outputTexture.name = "OutputTexture";

        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }

        outputTexture.Apply();

        outputImage.texture = outputTexture;
    }

    // Atualiza a imagem de saída com a cor atual
    private void UpdateOutputImage()
    {
        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }

        outputTexture.Apply();

        // Atualiza campo hexadecimal
        hexInputField.text = ColorUtility.ToHtmlStringRGB(currentColor);
        // Atualiza imagem de visualização
        currentImage.color = currentColor;
    }

    // Define valores de saturação e valor
    public void SetSV(float s, float v)
    {
        currentSat = s;
        currentVal = v;

        UpdateOutputImage();
    }

    // Atualiza imagem S/V quando a matiz muda
    public void UpdateSVImage()
    {
        currentHue = hueSlider.value;

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y / svTexture.height));
            }
        }

        svTexture.Apply();

        UpdateOutputImage();
    }

    // Processa input hexadecimal
    public void OnTextInput()
    {
        if (hexInputField.text.Length < 6)
            return;

        Color newCol;

        // Tenta converter hexadecimal para cor
        if (ColorUtility.TryParseHtmlString($"#{hexInputField.text}", out newCol))
            Color.RGBToHSV(newCol, out currentHue, out currentSat, out currentVal);

        hueSlider.value = currentHue;

        hexInputField.text = "";

        UpdateOutputImage();
    }
}
