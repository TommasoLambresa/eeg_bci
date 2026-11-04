using UnityEngine;
using TMPro;

public enum LegType
{
    LEFT,
    RIGHT,
    BOTH
}

// O Script serve para setar os parametros para o medidor
public class Medidor : MonoBehaviour
{
    [HideInInspector] public PlayerController _player;

    [HideInInspector] public float inputValue;
    [HideInInspector] public float[] inputAngles;

    public MedidorMovement medidor;

    [HideInInspector] public StartGame startGame;

    private void Update()
    {
        if (!_player) _player = FindAnyObjectByType<PlayerController>();
        if (!_player) return;

        // Obtém valores de input baseado no tipo de perna
        float[] values = _player.GetInputValues();

        var legType = UIConfigs.instance._legType;
        inputValue = legType == LegType.LEFT ? values[0] : values[1];

        if (inputAngles.Length < 1) 
            inputAngles = _player.GetInputAngles();
    }

    // Configura parametros do medidor
    public void SetParameters(StartDois start, float height, float tempoPrecisao)
    {
        startGame = start;
        medidor.SetMargemAcerto(height);
        medidor._tempoPrecisao = tempoPrecisao;
    }
}
