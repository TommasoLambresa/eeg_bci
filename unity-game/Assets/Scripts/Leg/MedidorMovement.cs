using UnityEngine;
using UnityEngine.UI;

public class MedidorMovement : MonoBehaviour
{
    public RectTransform _marker,
        _markerAngle;

    public float _heightMedidor => GetComponent<RectTransform>().rect.height;
    public float _sizeMarker => _marker.rect.height;
    public float _sizeMarkerAngle => _markerAngle.rect.height;

    [HideInInspector] public float _tempoPrecisao;

    [HideInInspector] public float positionMinYM; // Posição mínima marcador
    [HideInInspector] public float positionMaxYM; // Posição máxima marcador
    [HideInInspector] public float positionMinYMM; // Posição mínima angulo
    [HideInInspector] public float positionMaxYMM; // Posição máxima angulo

    public Medidor _medidor; // Referencia ao medidor principal

    public bool entrouOver = false; // Flag de sobreposição

    private bool _waitingForMinimum = false; // Esperando tempo mínimo

    public float _delayMaxTime = 3f; // Tempo máximo de delay
    private float _delayingTime; // Tempo atual de delay
    private bool _inDelay; // Em delay

    private void Start()
    {
        _medidor = GetComponentInParent<Medidor>();
    }

    private void Update()
    {
        if (!StartGame.instance || !StartGame.instance.GetStats())
        {
            contTempo = 0;
            return;
        }

        if (!GrazController.instance.OnFeedbackPhase)
            return;

        // Durante delay, reseta posição
        if (_inDelay)
        {
            UpdatePosition(_marker, positionMinYM, positionMaxYM, 0);

            _delayingTime += Time.deltaTime;

            if (_delayingTime >= _delayMaxTime)
            {
                _inDelay = false;
                _delayingTime = 0;
            }

            return;
        }

        // Atualiza posição do marcador baseado no input
        UpdatePosition(_marker, positionMinYM, positionMaxYM, _medidor.inputValue);

        bool isOverlapping = IsOverlapping();

        // Reset tempo se começou a sobrepor
        if (isOverlapping && !_wasOverlapping)
            _overlappingTime = 0;

        if (isOverlapping)
        {
            entrouOver = true;

            if (!_waitingForMinimum)
                _overlappingTime += Time.deltaTime;

            // Confirma se atingiu tempo mínimo
            if (_overlappingTime >= _tempoPrecisao)
            {
                _waitingForMinimum = true;
                ConfirmOverlapping();
                entrouOver = false;
                FeedbackController.instance.PlayParabens();
                RealtimeLog.instance._acerto = "ACERTOU";
            }
        }
        else if (entrouOver)
        {
            // Saiu da região antes do tempo mínimo
            ConfirmOverlapping();
            entrouOver = false;
            FeedbackController.instance.PlayErrou();
            RealtimeLog.instance._acerto = "ERROU";
        }
        else
            _waitingForMinimum = false;

        _wasOverlapping = isOverlapping;
    }

    int contTempo; // Contador de tempos

    // Confirma sobreposição e prepara próximo trial
    public void ConfirmOverlapping()
    {
        // Atualiza marcador de ângulo para próximo trial
        UpdatePosition(_markerAngle, positionMinYMM, positionMaxYMM, GameUIDois.Instance._anguloPerna[_medidor.startGame.actTrials] / 180);

        // Avança para próximo tempo de precisão
        int contTemposMax = GameUIDois.Instance._temposPrecisao.Count;
        contTempo = contTempo >= contTemposMax ? 0 : contTempo + 1;
        _tempoPrecisao = GameUIDois.Instance._temposPrecisao.Count > 1
            ? GameUIDois.Instance._temposPrecisao[contTempo]
            : GameUIDois.Instance._temposPrecisao[0];
        _overlappingTime = 0;
        _waitingForMinimum = false;
        _inDelay = true;
    }

    // Atualiza posição de um RectTransform
    public void UpdatePosition(RectTransform rt, float positionMin, float positionMax, float value)
    {
        float posY = Mathf.Lerp(positionMin, positionMax, value);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, posY);
    }

    // Calcula posições dos marcadores
    public void MarkerPosition()
    {
        positionMinYM = -(_heightMedidor - _sizeMarker) / 2f;
        positionMaxYM = (_heightMedidor - _sizeMarker) / 2f;

        positionMinYMM = -(_heightMedidor - _sizeMarkerAngle) / 2f;
        positionMaxYMM = (_heightMedidor - _sizeMarkerAngle) / 2f;

        UpdatePosition(_marker, positionMinYM, positionMaxYM, 0);
        UpdatePosition(_markerAngle, positionMinYMM, positionMaxYMM, 0);
    }

    // Configura margem de acerto
    public void SetMargemAcerto(float height)
    {
        _markerAngle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        MarkerPosition();
    }

    #region OVERLAP CHECK

    private float _overlappingTime; // Tempo de sobreposição
    private bool _wasOverlapping; // Flag de sobreposição anterior

    // Verifica se marcadores estão sobrepostos
    private bool IsOverlapping()
    {
        Rect marcador = GetRectScreenSpace(_marker);
        Rect regiao = GetRectScreenSpace(_markerAngle);

        // Adiciona margem para detecção
        float overlapMargin = 5f;
        marcador = new Rect(
            marcador.x - overlapMargin,
            marcador.y - overlapMargin,
            marcador.width + 2 * overlapMargin,
            marcador.height + 2 * overlapMargin
            );

        return marcador.Overlaps(regiao);
    }

    // Converte RectTransform para Rect em espaço de tela
    private Rect GetRectScreenSpace(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[2]);

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    #endregion
}
