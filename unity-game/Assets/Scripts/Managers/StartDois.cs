using UnityEngine;

public class StartDois : StartGame
{
    public override void Start()
    {
        base.Start();
        _log.experienciaChoice = RealtimeLog.ExperienciaChoice.Dois; // Define tipo de experiencia
    }

    private void Update()
    {
        // Inicializa com barra de espaço
        if (Input.GetKeyDown(KeyCode.Space) && !isInitialize && UIConfigs.instance && Started)
        {
            UIConfigs.instance.StartLogRealtime();

            // Configura medidor com angulo inicial
            var medidor = statusUI.medidor;
            medidor.gameObject.SetActive(true);
            medidor.medidor.UpdatePosition(medidor.medidor._markerAngle, medidor.medidor.positionMinYMM, medidor.medidor.positionMaxYMM, GameUIDois.Instance._anguloPerna[actTrials] / 180);

            statusUI.configs.SetActive(false);

            actTrials = 0;
            isInitialize = true;
            UIConfigs.instance.spaceBar.SetActive(false);

            GrazController.instance.StartProtocol(); // Inicia protocolo GRAZ
        }
    }

    public override void RestartGame()
    {
        base.RestartGame();
        statusUI.medidor.gameObject.SetActive(false); // Esconde medidor
    }

    // Configura altura e tempo de precisão do medidor
    public void HeightMargem(float _height, float _tempoPrecisao) 
    {
        statusUI.medidor.SetParameters(this, _height, _tempoPrecisao);
    }
}
