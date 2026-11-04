using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;

public class GameUIDois : UIConfigs
{
    #region SINGLETON

    private static GameUIDois _instance;

    public static GameUIDois Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GameUIDois>();

            return _instance;
        }
    }

    #endregion

    [HideInInspector] public StartDois startDois;
    private Experiment2RedesignedMenu redesignedMenu;

    // Valores padr�o
    [SerializeField] private float defaultGravidade = 9.71f;
    [SerializeField] private float defaultMargem = 1f;
    [SerializeField] private float defaultEspera = 3f;
    [SerializeField] private int defaultMaxAngle = 180;
    [SerializeField] private int defaultMinAngle = 90;

    [Space, Header("Perna")]
    public float _rotacaoPerna; // Rota��o atual da perna
    [SerializeField] private Toggle isRandomicAngle; // Flag angulos randomicos
    [SerializeField] private RectTransform _inputsTransformAngle; // Container de inputs de angulo
    [SerializeField] private GameObject _objInputRandomAngle; // Prefab input randomico
    [SerializeField] private GameObject _objInputTrialAngle; // Prefab input por trial
    [SerializeField] private MenuBtnOperation coordsButtonAngle; // Bot�o de coordenadas

    [Space, Header("Gravidade")]
    [SerializeField] private float _gravidade;
    [SerializeField] private TMP_InputField gravidadeField;

    [Space, Header("Medidor")]
    [SerializeField] private GameObject medidorUI; // UI do medidor

    [Space]
    [SerializeField] private TMP_InputField margemField; // Input margem de erro
    [SerializeField] private TMP_InputField tempoPrecisaoField; // Input tempo de precis�o
    [SerializeField] private Toggle isSplitTasks; // Toggle divis�o de tarefas
    [SerializeField] private RectTransform _inputsTransformTask; // Container de tarefas
    [SerializeField] private GameObject _objInputSliceTask; // Prefab de varias tarefas
    [SerializeField] private GameObject _objInputTask; // Prefab tarefa �nica
    [SerializeField] private MenuBtnOperation coordsButtonTask; // Bot�o de tarefas

    [HideInInspector] public float[] _anguloPerna; // Array de angulos por trial
    public List<float> _temposPrecisao; // Lista de tempos de precis�o

    private int trialsTaskCont = 0;

    public override void Awake()
    {
        base.Awake();
        startDois = FindAnyObjectByType<StartDois>();
    }

    public override void Start()
    {
        base.Start();
        SetTrials("1");
        SetRandomAngle(isRandomicAngle.isOn);
        
        // Garante que o modo Default esteja ativo e preenchido
        isDefault.isOn = true;
        SetDefault(true);

        SetTarefas(false);
        medidorUI.SetActive(false);

        // Garante que o boto "Entrar no modo" esteja vinculado corretamente
        var btnObj = GameObject.Find("=====UI=====/Canvas/Iniciar/Background");
        if (btnObj != null)
        {
            var btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(StartGame);
        }

        // Define um caminho de log padro caso esteja vazio para evitar travamentos
        if (string.IsNullOrEmpty(_pathLog))
        {
            _pathLog = Application.persistentDataPath + "/BCI_Log_Default.csv";
            if (_pathMesh != null) _pathMesh.text = _pathLog;
        }

        EnsureRedesignedMenu();
    }

    private void EnsureRedesignedMenu()
    {
        if (redesignedMenu == null)
            redesignedMenu = GetComponent<Experiment2RedesignedMenu>();
        if (redesignedMenu == null)
            redesignedMenu = gameObject.AddComponent<Experiment2RedesignedMenu>();

        redesignedMenu.Initialize(this);
    }

    public override void Update()
    {
        base.Update();

        // Ajusta altura dos containers
        AdjustContainerHeight(_inputsTransformAngle);
        AdjustContainerHeight(_inputsTransformAngle.parent.GetComponent<RectTransform>());
        AdjustContainerHeight(_inputsTransformTask);
        AdjustContainerHeight(_inputsTransformTask.parent.GetComponent<RectTransform>());
    }

    // Configura valores padr�o ou limpa os campos
    public override void SetDefault(bool isDefault)
    {
        base.SetDefault(isDefault);

        if (isDefault)
        {
            gravidadeField.text = defaultGravidade.ToString();
            margemField.text = defaultMargem.ToString();
            tempoPrecisaoField.text = defaultEspera.ToString();
            isRandomicAngle.isOn = true;
        }
        else
        {
            gravidadeField.text = "9,81";
            margemField.text = "7,5";
            tempoPrecisaoField.text = "3";
            isRandomicAngle.isOn = false;
        }
    }

    // Configura o n�mero de trials e atualiza a UI
    public override void SetTrials(string trials)
    {
        base.SetTrials(trials);

        if (!isRandomicAngle.isOn)
            SetRandomAngle(false);
    }

    // Define gravidade da f�sica
    public void SetGravidade(string gravidade)
    {
        if (string.IsNullOrEmpty(gravidade))
            return;
        _gravidade = float.Parse(gravidade);
    }

    // Configura rota��o da perna baseado nas entradas da UI
    public bool SetRotacaoPerna()
    {
        if (string.IsNullOrEmpty(gravidadeField.text) || gravidadeField.text == "0" || string.IsNullOrEmpty(margemField.text) || margemField.text == "0")
            return false;
        if (string.IsNullOrEmpty(_pathLog))
            return false;

        int trials = int.Parse(trialsField.text);
        float margem = float.Parse(margemField.text) * 10;
        _anguloPerna = new float[trials];
        _temposPrecisao = new List<float>();

        // Modo randomico para angulos
        if (isRandomicAngle.isOn)
        {
            var inputCs = _inputsTransformAngle.GetComponentInChildren<InputRandomCS>();

            if (inputCs == null || string.IsNullOrEmpty(inputCs._maxX.text) || string.IsNullOrEmpty(inputCs._maxY.text))
                return false;

            var valueMin = float.Parse(inputCs._maxY.text);
            var valueMax = float.Parse(inputCs._maxX.text);

            for (int i = 0; i < _anguloPerna.Length; i++)
                _anguloPerna[i] = UnityEngine.Random.Range(valueMin, valueMax) - 90f;
        }
        // Modo manual para angulos
        else
        {
            var inputCs = _inputsTransformAngle.GetComponentsInChildren<InputRandomCS>();

            for (int i = 0; i < _anguloPerna.Length; i++)
            {
                if (string.IsNullOrEmpty(inputCs[i]._maxX.text))
                    return false;

                _anguloPerna[i] = float.Parse(inputCs[i]._maxX.text) - 90f;
            }
        }

        // Configura tempos de precis�o baseado em divis�o de tarefas
        if (isSplitTasks.isOn)
        {
            var inputCs = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();

            for (int i = 0; i < _inputsTransformTask.childCount; i++)
            {
                if (string.IsNullOrEmpty(inputCs[i]._maxX.text) || string.IsNullOrEmpty(inputCs[i]._maxY.text))
                    return false;

                // Adiciona m�ltiplos tempos baseado na quantidade
                for (int z = 0; z < int.Parse(inputCs[i]._maxY.text); z++)
                {
                    _temposPrecisao.Add(float.Parse(inputCs[i]._maxX.text));
                }
            }
        }
        else
        {
            var inputCs = _inputsTransformTask.GetComponentInChildren<InputRandomCS>();

            _temposPrecisao.Add(float.Parse(inputCs._maxX.text));
        }

        SetGraz();

        startDois.trials = trials;
        startDois.HeightMargem(margem, _temposPrecisao[0]);
        _rotacaoPerna = _anguloPerna[0];

        Physics.gravity = new Vector3(Physics.gravity.x, -_gravidade, Physics.gravity.z);

        CheckFeedbackFrases();

        return true;
    }

    // Configura modo de angulo
    public void SetRandomAngle(bool isRandom)
    {
        GameObject objInput = isRandom ? _objInputRandomAngle : _objInputTrialAngle;

        if (isRandom) inputRandom = objInput.GetComponent<InputRandomCS>();
        if (inputRandom && isDefault.isOn)
        {
            inputRandom._maxX.text = defaultMaxAngle.ToString();
            inputRandom._maxY.text = defaultMinAngle.ToString();
        }
        else if (inputRandom && !isDefault.isOn)
        {
            inputRandom._maxX.text = "";
            inputRandom._maxY.text = "";
        }

        // Limpa inputs existentes
        while (_inputsTransformAngle.childCount > 0)
        {
            Transform child = _inputsTransformAngle.GetChild(0);
            child.SetParent(null); // Remove do parent primeiro
            Destroy(child.gameObject); // Destr�i no final do frame
        }

        trialsAngleCont = 1;
        // Adiciona novos inputs baseado no modo
        if (objInput == _objInputTrialAngle)
            while (trialsAngleCont <= startGame.trials)
                AddObjAngle(objInput);
        else
            AddObjAngle(objInput);

        inputRandom = null;
        coordsButtonAngle.gameObject.SetActive(!isRandom);
    }

    // Adiciona input de angulo
    public void AddObjAngle(GameObject objInput)
    {
        GameObject input = Instantiate(objInput);
        input.transform.parent = _inputsTransformAngle;

        if (input.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI _mesh))
            _mesh.text = $"Trial: {trialsAngleCont}";

        if (!isRandomicAngle.isOn && _inputsTransformAngle.childCount > startGame.trials)
            trialsField.text = _inputsTransformAngle.childCount.ToString();

        trialsAngleCont++;
    }

    // Configura divis�o de tarefas
    public void SetTarefas(bool on)
    {
        GameObject objInput = on ? _objInputSliceTask : _objInputTask;

        // Limpa tarefas existentes
        while (_inputsTransformTask.childCount > 0)
        {
            Transform child = _inputsTransformTask.GetChild(0);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        trialsTaskCont = 1;
        // Adiciona tarefas baseado no modo
        if (objInput == _objInputSliceTask)
            while (trialsTaskCont < 3)
                AddObjTask(objInput);
        else
            AddObjTask(objInput);

        coordsButtonTask.gameObject.SetActive(on);
    }

    // Adiciona input de tarefa
    public void AddObjTask(GameObject objInput)
    {
        GameObject input = Instantiate(objInput);
        input.transform.parent = _inputsTransformTask;

        if (input.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI _mesh))
        {
            if (objInput == _objInputSliceTask) _mesh.text = $"Tarefa: {trialsTaskCont}";
            else _mesh.text = $"Descanso";
        }

        int cont = trialsTaskCont;
        trialsTaskCont++;
    }

    public override void SetarConfigs()
    {
        base.SetarConfigs();
        spaceBar.SetActive(true);
        startGame.Started = true;
        configsUI.SetActive(true);
    }

    public override void StartGame()
    {
        if (!SetRotacaoPerna()) return;
        base.StartGame();
    }

    #region PATH

    public override void EndCoroutine()
    {
        base.EndCoroutine();

        // Default
        isDefault.isOn = loadedData.isDefault;
        
        // Trials
        trialsField.text = loadedData.trials + "";
        AdjustContainerHeight(_inputsTransformAngle);
        AdjustContainerHeight(_inputsTransformAngle.parent.GetComponent<RectTransform>());

        // Configura��es de angulo
        isRandomicAngle.isOn = loadedData.isRandomicAngle;
        if (isRandomicAngle.isOn)
        {
            var inputCs = _inputsTransformAngle.GetComponentInChildren<InputRandomCS>();
            if (inputCs == null) return;

            inputCs._maxX.text = loadedData.variations[0] + "";
            inputCs._maxY.text = loadedData.variations[1] + "";
        }
        else
        {
            var inputsCs = _inputsTransformAngle.GetComponentsInChildren<InputRandomCS>();
            var length = inputsCs.Length <= loadedData.variations.Length ? inputsCs.Length : loadedData.variations.Length;
            for (int i = 0; i < length; i++)
            {
                inputsCs[i]._maxX.text = loadedData.variations[i] + "";
            }
        }

        // Gravidade
        gravidadeField.text = loadedData.gravidade + "";
        
        // Margem
        margemField.text = loadedData.margem + "";

        isSplitTasks.isOn = loadedData.isSplitTasks;
        if (isSplitTasks.isOn)
        {
            var inputsCs = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();

            if (loadedData.totalDiv > inputsCs.Length)
            {
                for (int z = 2; z < loadedData.totalDiv; z++)
                    AddObjTask(_objInputSliceTask);

                AdjustContainerHeight(_inputsTransformTask);
                AdjustContainerHeight(_inputsTransformTask.parent.GetComponent<RectTransform>());

                inputsCs = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();
            }
            var length = inputsCs.Length <= loadedData.tempoEspera.Length ? inputsCs.Length : loadedData.tempoEspera.Length;
            for (int i = 0; i < length; i++)
                inputsCs[i]._maxX.text = loadedData.tempoEspera[i] + "";
        }
        else
        {
            tempoPrecisaoField.text = loadedData.tempoEspera[0] + "";
        }
    }

    #endregion

    public override void BackToMenuConfigs()
    {
        base.BackToMenuConfigs();
        startGame.RestartGame();
        configsUI.SetActive(false);
    }
}
