using UnityEngine;
using UnityEngine.UI;

// Script que controla
public class StartGame : MonoBehaviour
{
    private static StartGame _instance;

    public static StartGame instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<StartGame>();
            return _instance;
        }
    }

    public bool SetupDefault = false; // Flag de configuração padrão

    [HideInInspector] public StatusUIController statusUI;

    [HideInInspector] public bool isInitialize; // Se foi inicializado
    [HideInInspector] public bool Started; // Se está rodando

    [HideInInspector] public int trials = 25; // Total de trials
    public int actTrials = 0; // Trials atuais

    [HideInInspector] public RealtimeLog _log; // Sistema de log

    public virtual void Start()
    {
        _log = FindAnyObjectByType<RealtimeLog>();
    }

    // Reinicia o jogo
    public virtual void RestartGame()
    {
        actTrials = 0;
        isInitialize = false;
        Started = false;
        GrazController.instance.ToggleCanvas(false);
        LSLReceiver.instance.StopInlet(); // Para LSL
        FindAnyObjectByType<PlayerController>().UpdateLegValues(0, 0, 0); // Reseta pernas
        _log.EndLog(); // Finaliza log
    }

    // Para tudo que ta fazendo
    public virtual void StopTasks() { }

    // Obtém status de inicialização
    public bool GetStats()
    {
        return isInitialize;
    }
}
