using UnityEngine;
using UnityEngine.UI;

// Serve apenar para armazenar alguns objetos e usar em outro script
public class StatusUIController : MonoBehaviour
{
    public Medidor medidor;
    public GameObject configs;

    public Slider camRotation;
    public Slider camHorizontalMove;
    public Slider camVerticalMove;
    public Slider camZoom;

    public Slider inputLeft;
    public Slider inputRight;
}
