using UnityEngine;

// Controla a inicialização da Experiencia 1 (SEIDI)
public class StartUm : StartGame
{
    [Space, Header("Components")]
    public BallLauncher shootBall; // Referencia ao controlador de lançamento de bolas

    public override void Start()
    {
        base.Start();
        // Define o tipo de experiencia no sistema de logs
        _log.experienciaChoice = RealtimeLog.ExperienciaChoice.Um;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isInitialize && UIConfigs.instance && Started)
        {
            UIConfigs.instance.StartLogRealtime(); // Inicia o sistema de logging em tempo real
            statusUI.configs.SetActive(false); // Desativa o painel de configurações

            actTrials = 0; // Reinicia o contador de trials
            isInitialize = true; // Marca o jogo como inicializado
            UIConfigs.instance.spaceBar.SetActive(false); // Esconde a indicação da barra de espaço

            GrazController.instance.StartProtocol(); // Inicia o protocolo Graz
        }
    }

    // Método para parar todas as tarefas da experiencia
    public override void StopTasks()
    {
        base.StopTasks();
        shootBall.StopAllCoroutines(); // Para todas as corrotinas do lançador de bolas
    }
}
