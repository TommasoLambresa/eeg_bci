using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

// Classe para armazenar dados de configura��o
[System.Serializable]
public class Data
{
    public bool isDefault; // Se usa configura��es padr�o

    public int trials; // N�mero de trials

    // Experimento 1
    public float ballVelocity; // Velocidade da bola
    public float ballInterval; // Intervalo entre bolas

    public bool isRandomicPos; // Posi��es randomicas
    public float[] positions; // Array de posi��es

    // Experimento 2
    public string pernaEscolhida; // Perna escolhida
    public bool isRandomicAngle; // Angulos randomicos
    public int[] variations; // Varia��es

    public float gravidade; // Configura��o de gravidade

    public float margem; // Margem de erro
    public bool isSplitTasks; // Divis�o de tarefas
    public int totalDiv; // Total de divis�es
    public float[] tempoEspera; // Tempos de espera
}

// Enum para modos de jogo
public enum ModoJogo
{
    PRINCIPAL,
    TREINO
}

// Enum para tipos de UI
public enum UIModo
{
    Um,
    Dois
}

// Classe principal de controle de UI e configura��es
public class UIConfigs : MonoBehaviour
{
    #region SINGLETON

    private static UIConfigs _instance;

    public static UIConfigs instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<UIConfigs>();

            return _instance;
        }
    }

    #endregion

    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public StartGame startGame;
    [HideInInspector] public Dictionary<string, List<string>> dataInfos;

    public UIModo _ui;
    [HideInInspector] public ModoJogo _mode;
    public LegType _legType; // Tipo de perna selecionada

    [Space, Header("UI"), Space]
    public GameObject spaceBar; // Indicador da UI da barra de espa�o
    public CanvasGroup painelConfigs; // Grupo de canvas das configura��es
    public GameObject configsUI; // UI de configura��es

    public StatusUIController statusUI; // Controlador de status da UI

    public MenuBtnOperation confirmButton; // Bot�o de confirma��o

    // Campos para configura��o LSL
    public TMP_InputField inputStreamName,
        inputStreamType;

    // Frase de acerto/erro
    private string fraseAcerto,
        fraseErro;

    [Space, Header("Graz"), Space]
    public TMP_InputField _baselineField; // Input de dura��o da baseline
    public TMP_InputField _fixationField; // Input de dura��o da fixa��o
    public TMP_Text _refFixationText; // Texto de refer�ncia da fixa��o
    //public TMP_Text _refCueText;
    public TMP_InputField _feedbackField; // Input de dura��o do feedback
    public TMP_InputField _minPauseField; // Input de pausa m�nima
    public TMP_InputField _maxPauseField; // Input de pausa m�xima
    [SerializeField] private Image colorRefAcerto; // Cor do acerto
    [SerializeField] private Image colorRefErro; // Cor do erro

    // Sistema de configura��es de Cue
    [SerializeField] private RectTransform _inputsTransformCue;
    public List<GameObject> cueConfigs; // Lista de configura��es de cue
    [SerializeField] private GameObject curInputObj; // Prefab do cue
    public int cueCount = 1; // Contador de cues

    [Space, Header("Trials"), Space]
    public TMP_InputField trialsField;
    [HideInInspector] public int trialsCont = 0;
    [HideInInspector] public int trialsAngleCont = 0;

    #region CLIENTE

    [HideInInspector] public MenuBtnOperation actSkin; // Skin ativa

    #endregion

    [Space, Header("Path"), Space]
    [SerializeField] protected TextMeshProUGUI _pathMesh; // Texto do caminho
    private string archiveName; // Nome do arquivo

    [Header("Default"), Space]
    public Toggle isDefault; // Toggle de configura��o padr�o
    [SerializeField] private int defaultTrials = 25; // Trials padr�o

    [HideInInspector] public InputRandomCS inputRandom;

    private bool playerControllerBound;

    public virtual void Awake()
    {
        // Game UI, Gameplay e Experimento Um são carregados de forma aditiva.
        // Em algumas ordens de carregamento StartGame/PlayerController ainda não
        // existem quando este Awake é chamado. Portanto, nunca assumimos que
        // essas referências já estão disponíveis.
        TryBindRuntimeReferences();

        if (startGame == null || playerController == null)
            StartCoroutine(BindRuntimeReferencesWhenAvailable());
    }

    private bool TryBindRuntimeReferences()
    {
        if (startGame == null)
            startGame = FindAnyObjectByType<StartGame>();

        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (startGame != null && statusUI != null)
            startGame.statusUI = statusUI;

        if (playerController != null && statusUI != null && !playerControllerBound)
        {
            playerController.StartPlayerController(statusUI);
            playerController.CameraSetup();
            playerControllerBound = true;
        }

        // O Game 1 BCI não usa mais este receiver para controlar o avatar,
        // mas mantemos estes campos funcionais para o Game 2/UI legado.
        var lsl = FindAnyObjectByType<LSLReceiver>();

        if (inputStreamName != null && lsl != null)
            inputStreamName.text = lsl.streamName;

        if (inputStreamType != null && lsl != null)
            inputStreamType.text = lsl.streamType;

        return startGame != null && playerController != null;
    }

    private IEnumerator BindRuntimeReferencesWhenAvailable()
    {
        while (!TryBindRuntimeReferences())
            yield return null;
    }

    public virtual void Start()
    {
        TryBindRuntimeReferences();

        if (spaceBar != null)
            spaceBar.SetActive(false);

        if (startGame != null)
            startGame.Started = false;

        if (configsUI != null)
            configsUI.SetActive(false);

        if (playerController != null)
            playerController.LegChoice("nenhuma");
    }

    public virtual void Update()
    {
        // Atualiza referências caso outra cena aditiva tenha acabado de carregar.
        if (startGame == null || playerController == null)
            TryBindRuntimeReferences();

        // Tecla X para voltar ao menu de configurações
        if (startGame != null && startGame.Started && Input.GetKeyDown(KeyCode.X))
            BackToMenuConfigs();

        // O menu simples do Game 1 pode ocultar partes da UI antiga.
        if (confirmButton != null)
        {
            confirmButton.enabled = actSkin != null;

            Image confirmImage = confirmButton.GetComponent<Image>();
            if (confirmImage != null)
            {
                confirmImage.color = actSkin != null
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 160);
            }
        }

        if (_inputsTransformCue != null)
        {
            AdjustContainerHeight(_inputsTransformCue);

            if (_inputsTransformCue.parent != null)
                AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());
        }
    }

    private PhotoMode photoMode;

    // Define o modo de jogo (treino/principal)
    public void SetMode(string mode) => _mode = mode == "treino" ? ModoJogo.TREINO : ModoJogo.PRINCIPAL;

    // Define qual perna vai usar baseado no dropdown
    public void SetLegType(TMP_Dropdown pernaDropdown)
    {
        var _leg = GetLeg(pernaDropdown);
        playerController.LegChoice(_leg);

        if (_leg.Equals("esquerda"))
            _legType = LegType.LEFT;
        else if (_leg.Equals("direita"))
            _legType = LegType.RIGHT;
        else
            _legType = LegType.BOTH;
    }

    // Obt�m texto da perna selecionada no dropdown
    public string GetLeg(TMP_Dropdown pernaDropdown)
    {
        return pernaDropdown.options[pernaDropdown.value].text.ToLower();
    }

    // Verifica se modo foto est� ativo
    public bool GetPhotoActive()
    {
        if (photoMode == null)
            photoMode = GetComponent<PhotoMode>();

        return photoMode != null && photoMode.modeActive;
    }

    // Configura valores padr�o ou limpa campos
    public virtual void SetDefault(bool useDefault)
    {
        if (trialsField != null)
            trialsField.text = useDefault ? defaultTrials.ToString() : "1";

        if (useDefault)
        {
            if (startGame == null)
                startGame = FindAnyObjectByType<StartGame>();

            if (startGame != null)
                startGame.SetupDefault = true;
        }
    }

    // Define n�mero de trials
    public virtual void SetTrials(string trials)
    {
        if (string.IsNullOrEmpty(trials))
            return;

        if (startGame == null)
            startGame = FindAnyObjectByType<StartGame>();

        if (startGame != null && int.TryParse(trials, out int parsedTrials))
            startGame.trials = parsedTrials;
    }

    // Adiciona novo input de cue a UI
    public void AddCueInput()
    {
        GameObject input = Instantiate(curInputObj);
        input.transform.parent = _inputsTransformCue;

        var config = input.GetComponent<CueConfigs>();
        config.minusBtn.onClick.AddListener(() =>
        {
            RemoveCue(input);
        });

        config.imageBtn.onClick.AddListener(() =>
        {
            CarregarImagem(config);
        });

        cueConfigs.Add(input);

        cueCount++;
    }

    // Remove cue da UI
    public void RemoveCue(GameObject obj)
    {
        Destroy(obj);

        AdjustContainerHeight(_inputsTransformCue);
        AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());

        cueConfigs.Remove(obj);

        cueCount--;
    }

    // Ajusta altura dos containers da UI
    public void AdjustContainerHeight(RectTransform rect)
    {
        if (rect == null)
            return;

        float totalHeight = 0f;
        int activeChildren = 0;

        // Soma altura de todos os filhos ativos
        foreach (RectTransform child in rect)
        {
            if (!child.gameObject.activeSelf) continue;

            totalHeight += child.sizeDelta.y;
            activeChildren++;
        }

        // Adiciona espa�amento entre filhos
        if (activeChildren > 1)
        {
            totalHeight += (activeChildren - 1);
        }

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, totalHeight);
    }

    // Prepara configura��es
    public virtual void SetarConfigs()
    {
        if (painelConfigs != null)
            painelConfigs.alpha = 0;
    }

    [HideInInspector] public string _path; // Caminho atual
    [HideInInspector] public string _pathFolder; // Pasta selecionada
    [HideInInspector] public string _pathLog; // Caminho do log

    // Formata informa��es de arquivo para dicion�rio
    public Dictionary<string, List<string>> FormatInfos(string path)
    {
        Dictionary<string, string> infos = new Dictionary<string, string>();
        string currentKey = "";
        System.Text.StringBuilder currentText = new System.Text.StringBuilder();

        foreach (string line in File.ReadAllLines(path))
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                if (currentKey != "")
                {
                    infos[currentKey] = currentText.ToString();
                    currentText.Clear();
                }
                currentKey = line.Trim('[', ']');
            }
            else
            {
                currentText.AppendLine(line);
            }
        }

        if (currentKey != "")
        {
            infos[currentKey] = currentText.ToString();
        }

        Dictionary<string, List<string>> finalInfos = new Dictionary<string, List<string>>();
        foreach (var info in infos)
        {
            StringReader reader = new StringReader(info.Value);

            List<string> linhas = new List<string>();
            string linha;
            while (!string.IsNullOrWhiteSpace((linha = reader.ReadLine())))
                linhas.Add(linha);

            List<string> value = new List<string>();
            
            foreach (var l in linhas)
                value.Add(l);
            
            finalInfos[info.Key] = value;
        }

        return finalInfos;
    }

    // Inicia o jogo
    public virtual void StartGame()
    {
        var lsl = LSLReceiver.instance;
        lsl.streamName = inputStreamName.text;
        lsl.streamType = inputStreamType.text;
        lsl.CallInitialize(this);
    }

    // Configura parametros do protocolo Graz
    public void SetGraz()
    {
        var graz = GrazController.instance;
        if (_mode == ModoJogo.TREINO) graz.ToggleCanvas(true);
        graz.baselineDuration = string.IsNullOrEmpty(_baselineField.text) ? 10f : float.Parse(_baselineField.text);
        graz.fixationDuration = string.IsNullOrEmpty(_fixationField.text) ? 3f : float.Parse(_fixationField.text);
        //graz.cueDuration = string.IsNullOrEmpty(_cueField.text) ? 5f : float.Parse(_cueField.text);
        graz.feedbackDuration = string.IsNullOrEmpty(_feedbackField.text) ? 3.75f : float.Parse(_feedbackField.text);
        graz.minPauseDuration = string.IsNullOrEmpty(_minPauseField.text) ? 1.5f : float.Parse(_minPauseField.text);
        graz.maxPauseDuration = string.IsNullOrEmpty(_maxPauseField.text) ? 3.5f : float.Parse(_maxPauseField.text);
    }

    // Volta para menu de configura��es
    public virtual void BackToMenuConfigs()
    {
        GrazController.instance.EndProtocol();
        painelConfigs.alpha = 1;
    }

    private RealtimeLog _realtime;

    // Inicia sistema de logging em tempo real
    public virtual void StartLogRealtime()
    {
        if (_realtime == null)
            _realtime = FindAnyObjectByType<RealtimeLog>();

        _realtime.StartPath(_pathLog);
    }

    #region PATH

    // Carrega informa��es de arquivo JSON
    public void CarregarInfos()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        StartCoroutine(ShowLoadDialogCoroutine(EndCoroutine));
    }

    [HideInInspector] public Data loadedData;

    // Corrotina para carregar o arquivo
    private IEnumerator ShowLoadDialogCoroutine(Action onComplete)
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione uma arquivo JSON", "Selecionar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                _path = FileBrowser.Result[i];

            string jsonContent = File.ReadAllText(_path);
            loadedData = JsonUtility.FromJson<Data>(jsonContent);

            onComplete?.Invoke();
        }
    }

    // Carrega imagem para cue
    public void CarregarImagem(CueConfigs cue)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png", ".jpeg"));
        FileBrowser.SetDefaultFilter(".jpg");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        StartCoroutine(ShowLoadImageCoroutine(cue));
    }

    // Corrotina para carregar imagem
    private IEnumerator ShowLoadImageCoroutine(CueConfigs cue)
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione uma imagem", "Carregar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                _path = FileBrowser.Result[i];

            cue.path = _path; // Define caminho no cue
        }
    }

    // Callback quando carregamento termina
    public virtual void EndCoroutine() { }

    // Seta o nome do arquivo
    public void SetArchiveName(string name) => archiveName = name;

    // Seleciona pasta para salvar dados
    public void SelecionarPath()
    {
        if (string.IsNullOrEmpty(archiveName))
            return;
        FileBrowser.SetFilters(false);
        StartCoroutine(SelectPathCoroutine());
    }

    [HideInInspector] public Dictionary<string, List<string>> pathDataInfos;

    // Corrotina para selecionar pasta
    private IEnumerator SelectPathCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, null, null, "Selecione uma Pasta", "Selecionar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
            {
                _pathFolder = OnFilesSelected(FileBrowser.Result);
            }

            string archive = $"{_pathFolder}/{archiveName}.csv";

            _pathMesh.text = archive;
            _pathLog = archive;
        }
    }

    // Processa arquivos selecionados
    private string OnFilesSelected(string[] filePaths)
    {
        string filePath = filePaths[0];

        return filePath;
    }

    #endregion

    // Atualiza frase de feedback de acerto
    public void UpdateFeedbackAcerto(string str) 
    {
        fraseAcerto = str;
    }

    // Atualiza frase de feedback de erro
    public void UpdateFeedbackErro(string str)
    {
        fraseErro = str;
    }

    // Verifica e aplica frases de feedback
    public void CheckFeedbackFrases()
    {
        if (string.IsNullOrEmpty(fraseAcerto))
            fraseAcerto = "Parab�ns";
        if (string.IsNullOrEmpty(fraseErro))
            fraseErro = "Tente novamente";

        var feedback = FeedbackController.instance;
        feedback.SetParabens(fraseAcerto, colorRefAcerto.color);
        feedback.SetErrou(fraseErro, colorRefErro.color);
    }

    // Altera o genero do personagem
    public void ChangeGen(int value)
    {
        PlayerController.instance.ChangeGen(value * 100);
    }
}