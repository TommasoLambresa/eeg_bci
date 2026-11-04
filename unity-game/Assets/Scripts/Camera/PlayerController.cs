using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Script respons�vel por controlar todo o tipo de coisa do jogador
public class PlayerController : MonoBehaviour
{
    #region SINGLETON

    private static PlayerController _instance;

    public static PlayerController instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<PlayerController>();
            return _instance;
        }
    }

    #endregion

    [Header("Personagem")]
    public GameObject[] _pernaNormal = new GameObject[2]; // Pernas normais (esquerda, direita)
    public GameObject[] _pernaProtese = new GameObject[2]; // Pernas com protese
    public GameObject[] _coxaProtese = new GameObject[2]; // Coxas com protese
    public GameObject[] _peProtese = new GameObject[2]; // Pes com protese

    [Space]
    public Renderer[] _handRenderer, // Renders das m�os
        _legRenderer; // Renders das pernas

    #region CAMERA

    [Space, Header("Camera"), Space]
    [SerializeField] private bool isLocked; // Controle de travamento de camera
    public Camera mainCam; // Camera principal
    
    [SerializeField] 
    private float mainCamRotateMin = 29f, // Limites de rota��o da camera
        mainCamRotateMax = 69f;

    [SerializeField] 
    private float mainCamHorizontalMoveMin = -.5f, // Limites de movimento horizontal da camera
        mainCamHorizontalMoveMax = .7f,
        mainCamVerticalMoveMin = -1f, // Limites de movimento vertical da camera
        mainCamVerticalMoveMax = 0f;

    public Camera lateralCam; // Camera lateral
    
    [SerializeField]
    private float latCamRotateMin = -14f, // Limites de rota��o da camera lateral
    latCamRotateMax = 14f,
        latCamZoomMin = 1f, // Limites de zoom
        latCamZoomMax = 2.5f;

    [SerializeField]
    private float latCamHorizontalMoveMin = -.3f, // Limites de movimento horizontal da camera lateral
        latCamHorizontalMoveMax = .88f,
        latCamVerticalMoveMin = -1.8f, // Limites de movimento vertical da camera lateral
        latCamVerticalMoveMax = -.6f;

    private StatusUIController configsUI; // Referencia para o script que armazena os objetos 

    // Sliders de controle de camera
    private Slider camRotation;
    private Slider camHorizontalMove;
    private Slider camVerticalMove;
    private Slider camZoom;

    // Sliders de input da perna
    private Slider inputLeft;
    private Slider inputRight;
    
    private Camera _camera; // Camera principal
    private float rotationX; // Rota��o x atual da camera
    
    private struct CameraViewState
    {
        public bool initialized;
        public float rotation;
        public float horizontal;
        public float vertical;
        public float zoom;
    }

    private CameraViewState mainCameraState;
    private CameraViewState lateralCameraState;
    private bool applyingCameraState;
private float cameraZPosition, // Posi��o z da camera
        cameraYPosition, // Posi��o y da camera
        cameraXPosition; // Posi��o x da camera

    #endregion

    #region LEGS

    [Space, Header("Legs"), Space]
    [SerializeField] private RaiseLeg _leftLeg; // Controle da perna esquerda
    [SerializeField] private RaiseLeg _rightLeg; // Controle da perna direita

    // M�todo que seleciona a perna que vai ser usada
    public void LegChoice(string leg)
    {
        switch (leg)
        {
            case "esquerda":
                // Configura a protese na perna esquerda
                _pernaNormal[0].SetActive(true);
                _pernaNormal[1].SetActive(false);

                _pernaProtese[0].SetActive(false);
                _pernaProtese[1].SetActive(true);

                _coxaProtese[0].SetActive(false);
                _coxaProtese[1].SetActive(true);

                _peProtese[0].SetActive(false);
                _peProtese[1].SetActive(true);
                break;
            case "direita":
                // Configura a protese na perna direita
                _pernaNormal[0].SetActive(false);
                _pernaNormal[1].SetActive(true);

                _pernaProtese[0].SetActive(true);
                _pernaProtese[1].SetActive(false);

                _coxaProtese[0].SetActive(true);
                _coxaProtese[1].SetActive(false);

                _peProtese[0].SetActive(true);
                _peProtese[1].SetActive(false);
                break;
            case "ambas":
                // Configura a protese em ambas as pernas
                _pernaNormal[0].SetActive(false);
                _pernaNormal[1].SetActive(false);

                _pernaProtese[0].SetActive(true);
                _pernaProtese[1].SetActive(true);

                _coxaProtese[0].SetActive(true);
                _coxaProtese[1].SetActive(true);

                _peProtese[0].SetActive(true);
                _peProtese[1].SetActive(true);
                break;
            default:
                // Configura para que n�o use protese
                _pernaNormal[0].SetActive(true);
                _pernaNormal[1].SetActive(true);

                _pernaProtese[0].SetActive(false);
                _pernaProtese[1].SetActive(false);

                _coxaProtese[0].SetActive(false);
                _coxaProtese[1].SetActive(false);

                _peProtese[0].SetActive(false);
                _peProtese[1].SetActive(false);
                break;
        }
    }

    #endregion

    // M�todos que ajustam o input das pernas via UI
    public void SetInputLeft(float value)
    {
        _leftLeg.inputValue = value;
    }

    public void SetInputRight(float value)
    {
        _rightLeg.inputValue = value;
    }

    #region LOOP

    private void Start()
    {
        _camera = mainCam;
        mainCam.gameObject.SetActive(true);
        lateralCam.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Atualiza o log com os inputs e angulos das pernas
        RealtimeLog.instance._inputPernas = new Vector2(GetInputValues()[0], GetInputValues()[1]);
        RealtimeLog.instance._pernaAngles = GetBothLegsAngles();
    }

    // M�todo que atualiza a rota��o x de acordo com o movimento do slide
private void OnSliderChanged(float value)
    {
        rotationX = value;
        UpdateCameraRot();
        SaveCurrentCameraState();
    }

    // Atualiza rota��o da camera baseada no valor do slider
    private void UpdateCameraRot()
    {
        if (_camera == null)
            return;
        
        if (_camera == mainCam)
            _camera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        else
            _camera.transform.localRotation = Quaternion.Euler(rotationX, -90f, 0f);
    }

    // Inicializa o controle do jogador com o StatusUI
    public void StartPlayerController(StatusUIController _configsUI)
    {
        configsUI = _configsUI;

        // Configura slider de rota��o da c�mera
        if (configsUI.camRotation != null)
        {
            camRotation = configsUI.camRotation;
            if (_camera == mainCam)
            {
                camRotation.minValue = mainCamRotateMin;
                camRotation.maxValue = mainCamRotateMax;
            }
            else
            {
                camRotation.minValue = latCamRotateMin;
                camRotation.maxValue = latCamRotateMax;
            }
            camRotation.onValueChanged.AddListener(OnSliderChanged);
        }

        // Configura slider de movimento horizontal
        if (configsUI.camHorizontalMove != null)
        {
            camHorizontalMove = configsUI.camHorizontalMove;
            camHorizontalMove.minValue = 0f;
            camHorizontalMove.maxValue = 1f;
            camHorizontalMove.onValueChanged.AddListener(OnMoveSliderChangedHO);

            // Configura a posi��o inicial
            if (_camera == mainCam)
                cameraZPosition = Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, camHorizontalMove.value);
            else
                cameraZPosition = Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, camHorizontalMove.value);
            UpdateHorizontalCameraPosition();
        }

        // Configura slider de movimento vertical
        if (configsUI.camVerticalMove != null)
        {
            camVerticalMove = configsUI.camVerticalMove;
            camVerticalMove.minValue = 0f;
            camVerticalMove.maxValue = 1f;
            camVerticalMove.onValueChanged.AddListener(OnMoveSliderChangedVE);

            // Configura a posi��o inicial
            if (_camera == mainCam)
                cameraYPosition = Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, camVerticalMove.value);
            else
                cameraYPosition = Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, camVerticalMove.value);
            UpdateVerticalCameraPosition();
        }

        // Configura slider de zoom (apenas para c�mera lateral)
        if (_camera != mainCam)
        {
            if (configsUI.camZoom != null)
            {
                camZoom = configsUI.camZoom;
                camZoom.minValue = 0f;
                camZoom.maxValue = 1f;
                camZoom.onValueChanged.AddListener(OnZoomSliderChanged);

                cameraXPosition = Mathf.Lerp(latCamZoomMin, latCamZoomMax, camZoom.value);
                UpdateZoomCameraPosition();
            }
        }

        // Configura sliders de input das pernas
        if (configsUI.inputLeft != null)
        {
            inputLeft = configsUI.inputLeft;
            inputLeft.minValue = 0f;
            inputLeft.maxValue = 1f;
            inputLeft.onValueChanged.AddListener(SetInputLeft);
        }

        if (configsUI.inputRight != null)
        {
            inputRight = configsUI.inputRight;
            inputRight.minValue = 0f;
            inputRight.maxValue = 1f;
            inputRight.onValueChanged.AddListener(SetInputRight);
        }
    }

    #endregion

    #region CAMERA FUNCTIONS

    // Alterna entre c�mera principal e lateral
public void SwitchCam()
    {
        var status = UIConfigs.instance.statusUI;
        SaveCurrentCameraState();

        if (_camera == mainCam)
        {
            _camera = lateralCam;
            mainCam.gameObject.SetActive(false);
            lateralCam.gameObject.SetActive(true);
            status.camZoom.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            _camera = mainCam;
            mainCam.gameObject.SetActive(true);
            lateralCam.gameObject.SetActive(false);
            status.camZoom.transform.parent.gameObject.SetActive(false);
        }

        StartPlayerController(status);
        CameraSetup();
    }

    // Configura valores padr�o da camera
public void CameraSetup()
    {
        EnsureCameraStates();

        CameraViewState state = _camera == mainCam ? mainCameraState : lateralCameraState;
        applyingCameraState = true;

        if (camRotation != null)
        {
            camRotation.SetValueWithoutNotify(state.rotation);
            rotationX = state.rotation;
            UpdateCameraRot();
        }

        if (camHorizontalMove != null)
        {
            camHorizontalMove.SetValueWithoutNotify(state.horizontal);
            cameraZPosition = _camera == mainCam
                ? Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, state.horizontal)
                : Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, state.horizontal);
            UpdateHorizontalCameraPosition();
        }

        if (camVerticalMove != null)
        {
            camVerticalMove.SetValueWithoutNotify(state.vertical);
            cameraYPosition = _camera == mainCam
                ? Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, state.vertical)
                : Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, state.vertical);
            UpdateVerticalCameraPosition();
        }

        if (camZoom != null)
        {
            camZoom.SetValueWithoutNotify(state.zoom);
            cameraXPosition = Mathf.Lerp(latCamZoomMin, latCamZoomMax, state.zoom);
            if (_camera != mainCam)
                UpdateZoomCameraPosition();
        }

        applyingCameraState = false;
        SaveCurrentCameraState();
    }

private void EnsureCameraStates()
    {
        if (!mainCameraState.initialized)
        {
            mainCameraState.initialized = true;
            mainCameraState.rotation = 41.33333f;
            mainCameraState.horizontal = 0.2672153f;
            mainCameraState.vertical = 1f;
            mainCameraState.zoom = 0.5f;
        }

        if (!lateralCameraState.initialized)
        {
            lateralCameraState.initialized = true;
            lateralCameraState.rotation = 0f;
            lateralCameraState.horizontal = 0.3f;
            lateralCameraState.vertical = 1f;
            lateralCameraState.zoom = 0.5f;
        }
    }

    private void SaveCurrentCameraState()
    {
        if (applyingCameraState)
            return;

        EnsureCameraStates();

        CameraViewState state = _camera == mainCam ? mainCameraState : lateralCameraState;
        if (camRotation != null)
            state.rotation = camRotation.value;
        if (camHorizontalMove != null)
            state.horizontal = camHorizontalMove.value;
        if (camVerticalMove != null)
            state.vertical = camVerticalMove.value;
        if (camZoom != null)
            state.zoom = camZoom.value;
        state.initialized = true;

        if (_camera == mainCam)
            mainCameraState = state;
        else
            lateralCameraState = state;
    }


    // M�todo que atualiza os movimento de acordo com o movimento dos sliders
private void OnMoveSliderChangedHO(float value)
    {
        if (_camera == mainCam)
            cameraZPosition = Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, value);
        else
            cameraZPosition = Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, value);
        UpdateHorizontalCameraPosition();
        SaveCurrentCameraState();
    }

private void OnMoveSliderChangedVE(float value)
    {
        if (_camera == mainCam)
            cameraYPosition = Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, value);
        else
            cameraYPosition = Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, value);
        UpdateVerticalCameraPosition();
        SaveCurrentCameraState();
    }

private void OnZoomSliderChanged(float value)
    {
        cameraXPosition = Mathf.Lerp(latCamZoomMin, latCamZoomMax, value);
        UpdateZoomCameraPosition();
        SaveCurrentCameraState();
    }

    // M�todos para atualizar posi��o da camera
    private void UpdateHorizontalCameraPosition()
    {
        if (_camera == null)
            return;
        Vector3 localPos = _camera.transform.localPosition;
        localPos.z = cameraZPosition;
        _camera.transform.localPosition = localPos;
    }

    private void UpdateVerticalCameraPosition()
    {
        if (_camera == null)
            return;
        Vector3 localPos = _camera.transform.localPosition;
        localPos.y = cameraYPosition;
        _camera.transform.localPosition = localPos;
    }

    private void UpdateZoomCameraPosition()
    {
        if (_camera == null)
            return;
        Vector3 localPos = _camera.transform.localPosition;
        localPos.x = cameraXPosition;
        _camera.transform.localPosition = localPos;
    }

    #endregion

    #region LSLReceiver

    // M�todo para atualizar valores das pernas
    public void UpdateLegValues(float rightLeg, float bothLegs, float leftLeg)
    {
        rightLeg = Mathf.Clamp01(rightLeg);
        bothLegs = Mathf.Clamp01(bothLegs);
        leftLeg  = Mathf.Clamp01(leftLeg);

        // Stream LSL:
        // canal 0 = direita
        // canal 1 = ambas
        // canal 2 = esquerda
        if (_rightLeg != null)
            _rightLeg.inputValue = Mathf.Max(rightLeg, bothLegs);

        if (_leftLeg != null)
            _leftLeg.inputValue = Mathf.Max(leftLeg, bothLegs);

        var realtime = RealtimeLog.instance;
        if (realtime != null && _leftLeg != null && _rightLeg != null)
            realtime._inputPernas = new Vector2(_leftLeg.inputValue, _rightLeg.inputValue);
    }

    #endregion

    #region GETHER

    // M�todos para obter informa��es sobre as pernas
    public int GetLegsValue()
    {
        int value = (int)_rightLeg.currentAngle;
        return value;
    }

    public float[] GetInputValues()
    {
        float[] values = { _leftLeg.inputValue, _rightLeg.inputValue };

        return values;
    }

    public float[] GetInputAngles()
    {
        // Gera angulos aleat�rios
        float leftAngle = Mathf.Clamp01((Random.Range(0, 65) - _leftLeg.minAngle) / (_leftLeg.maxAngle - _leftLeg.minAngle));
        float rightAngle = Mathf.Clamp01((Random.Range(0, 65) - _rightLeg.minAngle) / (_rightLeg.maxAngle - _rightLeg.minAngle));

        var _realtime = RealtimeLog.instance;
        _realtime._pernaAngles = new Vector2(leftAngle, rightAngle);

        float[] values = { leftAngle, rightAngle };
        return values;
    }

    public float GetLeftLegAngle()
    {
        if (_leftLeg != null)
        {
            float angle = _leftLeg.currentAngle;
            return Mathf.Clamp(angle, _leftLeg.maxAngle, _leftLeg.minAngle);
        }

        return 0;
    }

    public float GetRightLegAngle()
    {
        if (_rightLeg != null)
        {
            float angle = _rightLeg.currentAngle;
            return Mathf.Clamp(angle, _rightLeg.maxAngle, _rightLeg.minAngle);
        }

        return 0;
    }

    public Vector2 GetBothLegsAngles()
    {
        return new Vector2(GetLeftLegAngle(), GetRightLegAngle());
    }

    #endregion

    // M�todo para alterar caracter�sticas de genero usando blend shapes (MASC/FEM)
    public void ChangeGen(float value)
    {
        SkinnedMeshRenderer[] pernaRenderer = new SkinnedMeshRenderer[2];
        SkinnedMeshRenderer[] peRenderer = new SkinnedMeshRenderer[2];

        for (int i = 0; i < 2; i++)
        {
            pernaRenderer[i] = _pernaNormal[i].GetComponent<SkinnedMeshRenderer>();
            peRenderer[i] = _peProtese[i].GetComponent<SkinnedMeshRenderer>();
        }

        for (int i = 0; i < 2; i++)
        {
            pernaRenderer[i].SetBlendShapeWeight(2, value);
            peRenderer[i].SetBlendShapeWeight(0, value);
        }
    }
}