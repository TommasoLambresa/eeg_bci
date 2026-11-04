using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderValueDisplay : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI valueText;
    public string suffix = "";
    public string format = "F1"; // One decimal place

    void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(UpdateText);
            UpdateText(slider.value);
        }
    }

    public void UpdateText(float value)
    {
        if (valueText != null)
        {
            valueText.text = value.ToString(format) + suffix;
        }
    }
}
