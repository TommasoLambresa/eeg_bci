using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Classe principal da UI para a Experiencia 1 (SEIDI)
public class GameUIUm : UIConfigs
{
    #region SINGLETON

    private static GameUIUm _instance;

    public static GameUIUm Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GameUIUm>();

            return _instance;
        }
    }

    #endregion

    [HideInInspector] public StartUm startUm; // Referencia ao controlador da experiencia 1

    // Valores padr�o para configura��es
    [SerializeField] private float defaultVelocity = 3f;
    [SerializeField] private float defaultMaxValueX = 25f;
    [SerializeField] private float defaultMaxValueY = 25f;
    [SerializeField] private float defaultMinValueX = -25f;
    [SerializeField] private float defaultMinValueY = 5f;

    [Space, Header("Bola configs"), Space]
    [SerializeField] private TMP_InputField velocityField; // Campo de input para velocidade
    [SerializeField] private TMP_InputField intervalField; // Campo de input para intervalo
    [SerializeField] private float _moveSpeed; // Velocidade atual da bola

    [Space, Header("Posi��o da bola"), Space]
    [SerializeField] private Toggle isRandomic; // Toggle para posi��es randomicas
    [SerializeField] private RectTransform inputsTransform; // Container dos inputs
    [SerializeField] private GameObject objInputRandom; // Prefab do input randomico
    [SerializeField] private GameObject objInputTrial; // Prefab do input por trial
    [SerializeField] private Button coordsButton; // Bot�o de coordenadas

    [HideInInspector] public Vector2[] _posicoesBola; // Array com todas as posi��es da bola

    public override void Awake()
    {
        base.Awake();
        startUm = FindAnyObjectByType<StartUm>();
    }

    public override void Start()
    {
        base.Start();
        SetTrials("1");
        isDefault.isOn = true; // Define como configura��o padr�o
        Physics.gravity = new Vector3(Physics.gravity.x, -9.81f, Physics.gravity.z); // Configura gravidade
    }

    public override void Update()
    {
        base.Update();
        // Ajusta a altura dos containers da UI
        AdjustContainerHeight(inputsTransform);
        AdjustContainerHeight(inputsTransform.parent.GetComponent<RectTransform>());
    }

    // Configura valores padr�o ou limpa os campos
    public override void SetDefault(bool isDefault)
    {
        base.SetDefault(isDefault);

        if (isDefault)
        {
            velocityField.text = defaultVelocity.ToString();
            isRandomic.isOn = true;
        }
        else
        {
            velocityField.text = "0";
            isRandomic.isOn = false;
        }
    }

    // Configura o n�mero de trials e atualiza a UI
    public override void SetTrials(string trials)
    {
        base.SetTrials(trials);

        if (!isRandomic.isOn)
            SetRandomNoise(false);
    }

    // Define a velocidade de movimento da bola
    public void SetMoveSpeed(string moveSpeed)
    {
        if (string.IsNullOrEmpty(moveSpeed))
            return;
        _moveSpeed = float.Parse(moveSpeed);
    }

    // Configura o modo de ru�do (randomico ou trial)
    public void SetRandomNoise(bool isRandom)
    {
        GameObject objInput = isRandom ? objInputRandom : objInputTrial;

        if (isRandom) inputRandom = objInput.GetComponent<InputRandomCS>();
        
        // Configura valores padr�o nos campos se estiver no modo padr�o
        if (inputRandom && isDefault.isOn)
        {
            inputRandom._maxX.text = defaultMaxValueX.ToString();
            inputRandom._maxY.text = defaultMaxValueY.ToString();
            inputRandom._minX.text = defaultMinValueX.ToString();
            inputRandom._minY.text = defaultMinValueY.ToString();
        }
        // Limpa campos se n�o for modo padr�o
        else if (inputRandom && !isDefault.isOn)
        {
            inputRandom._maxX.text = "";
            inputRandom._maxY.text = "";
            inputRandom._minX.text = "";
            inputRandom._minY.text = "";
        }

        // Limpa todos os inputs existentes
        while (inputsTransform.childCount > 0)
        {
            Transform child = inputsTransform.GetChild(0);
            child.SetParent(null); // Remove do parent primeiro
            Destroy(child.gameObject); // Destr�i no final do frame
        }

        trialsCont = 1;
        // Cria inputs baseado no modo selecionado
        if (objInput == objInputTrial)
            while (trialsCont <= startGame.trials)
                AddObjInput(objInput);
        else
            AddObjInput(objInput);

        inputRandom = null;
        coordsButton.gameObject.SetActive(!isRandom); // Mostra/oculta bot�o de coordenadas
    }

    // Adiciona um novo input � UI
    public void AddObjInput(GameObject objInput)
    {
        GameObject input = Instantiate(objInput);
        input.transform.parent = inputsTransform;

        // Atualiza o texto do trial se for modo por trial
        if (input.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI _mesh))
            _mesh.text = $"Trial: {trialsCont}";

        // Atualiza contador de trials se necess�rio
        if (!isRandomic.isOn && inputsTransform.childCount > startGame.trials)
            trialsField.text = inputsTransform.childCount.ToString();

        trialsCont++;
    }

    // Configura as posi��es da bola baseado nas entradas da UI
    public bool SetPosicaoBola()
    {
        // Valida��es iniciais
        if (string.IsNullOrEmpty(velocityField.text) || velocityField.text == "0")
            return false;
        if (string.IsNullOrEmpty(_pathLog))
            return false;

        int trials = int.Parse(trialsField.text);
        _posicoesBola = new Vector2[trials];

        // Modo randomico
        if (isRandomic.isOn)
        {
            var inputCs = inputsTransform.GetComponentInChildren<InputRandomCS>();

            if (string.IsNullOrEmpty(inputCs._minX.text) || string.IsNullOrEmpty(inputCs._minY.text) || string.IsNullOrEmpty(inputCs._maxX.text) || string.IsNullOrEmpty(inputCs._maxY.text))
                return false;

            var minNoise = new Vector2(float.Parse(inputCs._minX.text), float.Parse(inputCs._minY.text));
            var maxNoise = new Vector2(float.Parse(inputCs._maxX.text), float.Parse(inputCs._maxY.text));

            for (int i = 0; i < _posicoesBola.Length; i++)
                _posicoesBola[i] = new Vector2(Random.Range(minNoise.x, maxNoise.x), Random.Range(minNoise.y, maxNoise.y));
        }
        // Modo manual
        else
        {
            var inputCs = inputsTransform.GetComponentsInChildren<InputRandomCS>();

            for (int i = 0; i < _posicoesBola.Length; i++)
            {
                if (string.IsNullOrEmpty(inputCs[i]._maxX.text) || string.IsNullOrEmpty(inputCs[i]._maxY.text))
                    return false;

                var valueX = float.Parse(inputCs[i]._maxX.text);
                var valueY = float.Parse(inputCs[i]._maxY.text);
                _posicoesBola[i] = new Vector2(valueX, valueY);
            }
        }

        //SetGraz(); // Configura sistema Graz

        configsUI.SetActive(false);
        startUm.shootBall.SetParameters(_moveSpeed, _posicoesBola);

        CheckFeedbackFrases(); // Verifica frases de feedback

        return true;
    }

    // Prepara a UI para iniciar as configura��es
    public override void SetarConfigs()
    {
        base.SetarConfigs();
        spaceBar.SetActive(true);
        configsUI.SetActive(true);
        startGame.Started = true;
    }

    // Volta para o menu de configura��es
    public override void BackToMenuConfigs()
    {
        base.BackToMenuConfigs();
        startGame.RestartGame();
        startGame.StopTasks();
        configsUI.SetActive(false);
    }

    // Inicia o jogo ap�s valida��es
    public override void StartGame()
    {
        if (!SetPosicaoBola()) return;
        base.StartGame();
    }

    #region PATH

    // Carrega configura��es salvas
    public override void EndCoroutine()
    {
        base.EndCoroutine();

        // Configura��o padr�o
        isDefault.isOn = loadedData.isDefault;

        // Trials
        trialsField.text = loadedData.trials + "";
        AdjustContainerHeight(inputsTransform);
        AdjustContainerHeight(inputsTransform.parent.GetComponent<RectTransform>());

        // Configura��es da bola
        velocityField.text = loadedData.ballVelocity + "";
        intervalField.text = loadedData.ballInterval + "";

        // Posi��es randomicas
        isRandomic.isOn = loadedData.isRandomicPos;
        if (isRandomic.isOn)
        {
            var inputCs = inputsTransform.GetComponentInChildren<InputRandomCS>();
            inputCs._maxX.text = loadedData.positions[0] + "";
            inputCs._maxY.text = loadedData.positions[1] + "";
            inputCs._minX.text = loadedData.positions[2] + "";
            inputCs._minY.text = loadedData.positions[3] + "";
        }
        else
        {
            // Carrega posi��es espec�ficas por trial
            var inputsCs = inputsTransform.GetComponentsInChildren<InputRandomCS>();
            var length = inputsCs.Length <= loadedData.positions.Length ? inputsCs.Length : loadedData.positions.Length;
            for (int i = 0; i < length; i++)
            {
                inputsCs[i]._maxX.text = loadedData.positions[i] + "";
                i++;
                inputsCs[i]._maxY.text = loadedData.positions[i] + "";
            }
        }
    }

    #endregion
}
