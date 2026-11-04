using System;
using System.Collections;
using UnityEngine;

public enum BallDirection
{
    Left   = 0,
    Center = 1,
    Right  = 2,
    Outside = 3
}

public class BallLauncher : MonoBehaviour
{
    #region SINGLETON

    private static BallLauncher _instance;

    public static BallLauncher instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<BallLauncher>();
            return _instance;
        }
    }

    #endregion

    #region TRANSFORMS

    private Transform origin;
    [SerializeField] private Transform[] targets;
    private Transform currentTarget;

    #endregion

    #region NOISE

    private Vector2[] noises;
    private int noiseIndex;

    #endregion

    [Header("Configurações")]
    public GameObject ballPrefab;
    public int resolution = 30;
    public bool isRendEnable = true;

    [Header("Trajetória")]
    [Tooltip("Altura adicional máxima da parábola, em unidades do Unity.")]
    [SerializeField, Min(0f)] private float trajectoryHeight = 0.75f;

    [Header("Segurança")]
    [SerializeField, Min(1f)] private float outcomeTimeout = 8f;

    [Header("Feedback visual legado")]
    [Tooltip("Mantém as mensagens antigas de acerto/erro. O Game 1 desativa isto temporariamente e usa o placar.")]
    [SerializeField] private bool showLegacyOutcomeFeedback = true;

    public bool ShowLegacyOutcomeFeedback
    {
        get => showLegacyOutcomeFeedback;
        set => showLegacyOutcomeFeedback = value;
    }

    private float ballSpeed = 1f;
    private LineRenderer trajectoryLine;
    private float currentHorizontalPeak;
    private float currentHeight;
    private Coroutine ballCoroutine;
    private Coroutine timeoutCoroutine;
    private BallController activeBall;

    [HideInInspector] public bool isLaunching;

    public bool HasActiveBall => activeBall != null;
    public BallDirection CurrentDirection { get; private set; }

    /// <summary>
    /// direction, hit. É disparado uma única vez para cada bola.
    /// </summary>
    public event Action<BallDirection, bool> BallResolved;

    private void Start()
    {
        origin = transform;
        trajectoryLine = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (StartGame.instance == null || !StartGame.instance.GetStats())
            return;

        bool shouldRender = isRendEnable && HasActiveBall;
        if (shouldRender)
            UpdateTrajectoryVisualization();

        if (trajectoryLine != null && trajectoryLine.enabled != shouldRender)
            trajectoryLine.enabled = shouldRender;
    }

    public void SetParameters(float moveSpeed, Vector2[] noise)
    {
        ballSpeed = Mathf.Max(0.01f, moveSpeed);
        noises = noise;
        noiseIndex = 0;
    }

    /// <summary>
    /// Mantém compatibilidade com o protocolo Graz antigo.
    /// Seleciona aleatoriamente apenas esquerda, centro ou direita.
    /// </summary>
    public GameObject LaunchSingleBall()
    {
        if (targets == null || targets.Length == 0)
            return null;

        int selectableTargets = Mathf.Min(3, targets.Length);
        int option = UnityEngine.Random.Range(0, selectableTargets);
        return LaunchBallInternal((BallDirection)option, -1f, true);
    }

    /// <summary>
    /// Lança uma bola para uma direção específica. arrivalTime é o tempo,
    /// em segundos, entre a saída do canhão e a chegada ao alvo.
    /// </summary>
    public GameObject LaunchBall(BallDirection direction, float arrivalTime)
    {
        return LaunchBallInternal(direction, arrivalTime, false);
    }

    private GameObject LaunchBallInternal(BallDirection direction, float arrivalTime, bool updateGrazCue)
    {
        if (HasActiveBall)
        {
            Debug.LogWarning("Já existe uma bola ativa. O novo comando foi ignorado.");
            return null;
        }

        int targetIndex = (int)direction;
        if (targets == null || targetIndex < 0 || targetIndex >= targets.Length || targets[targetIndex] == null)
        {
            Debug.LogError($"Alvo não configurado para a direção {direction} (índice {targetIndex}).");
            return null;
        }

        if (origin == null)
            origin = transform;

        CurrentDirection = direction;
        currentTarget = targets[targetIndex];
        SetTargetMetadata(direction, updateGrazCue);
        GeneratePeak();

        GameObject ball = Instantiate(ballPrefab, origin.position, Quaternion.identity);
        BallController controller = ball.GetComponent<BallController>();
        if (controller == null)
            controller = ball.AddComponent<BallController>();

        activeBall = controller;
        isLaunching = true;

        controller.Initialize(
            this,
            direction,
            origin,
            currentTarget,
            currentHeight,
            currentHorizontalPeak);

        ballCoroutine = StartCoroutine(MoveBallCoroutine(ball, controller, arrivalTime));
        controller.SetCoroutine(ballCoroutine);

        float timeout = Mathf.Max(1f, arrivalTime > 0f ? arrivalTime + outcomeTimeout : outcomeTimeout);
        timeoutCoroutine = StartCoroutine(OutcomeTimeout(controller, timeout));

        return ball;
    }

    private void SetTargetMetadata(BallDirection direction, bool updateGrazCue)
    {
        float cueAngle;
        string label;

        switch (direction)
        {
            case BallDirection.Left:
                cueAngle = 180f;
                label = "ESQUERDA";
                break;
            case BallDirection.Center:
                cueAngle = -90f;
                label = "CENTRO";
                break;
            case BallDirection.Right:
                cueAngle = 0f;
                label = "DIREITA";
                break;
            default:
                cueAngle = 90f;
                label = "FORA";
                break;
        }

        if (RealtimeLog.instance != null)
            RealtimeLog.instance._target = label;

        if (updateGrazCue && GrazController.instance != null)
            GrazController.instance.SetCueImageDirection(cueAngle);
    }

    private void GeneratePeak()
    {
        // Game 1: trajetória determinística em um plano vertical.
        // O noise lateral foi desativado porque criava uma curva 3D não física.
        currentHorizontalPeak = 0f;
        currentHeight = trajectoryHeight;
    }

    private void UpdateTrajectoryVisualization()
    {
        if (trajectoryLine == null || origin == null || currentTarget == null)
            return;

        int pointCount = Mathf.Max(2, resolution);
        Vector3[] points = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            points[i] = CalculateBallPosition(t);
        }

        trajectoryLine.positionCount = pointCount;
        trajectoryLine.SetPositions(points);
    }

    public Vector3 CalculateBallPosition(float t)
    {
        // Interpolação reta entre lançador e alvo.
        Vector3 basePos = Vector3.Lerp(origin.position, currentTarget.position, t);

        // Única curvatura adicional: vertical no eixo Y global.
        // Assim a trajetória permanece em um plano ortogonal ao chão.
        float parabola = 4f * currentHeight * t * (1f - t);

        return basePos + Vector3.up * parabola;
    }

    private IEnumerator MoveBallCoroutine(GameObject ball, BallController controller, float requestedArrivalTime)
    {
        float distance = Vector3.Distance(origin.position, currentTarget.position);
        float duration;

        if (requestedArrivalTime > 0f)
            duration = Mathf.Max(0.05f, requestedArrivalTime);
        else if (GrazController.instance != null && GrazController.instance.inGrazProtocol)
            duration = Mathf.Max(0.05f, GrazController.instance.cueDuration + 1f);
        else
            duration = distance / Mathf.Max(0.01f, ballSpeed);

        float effectiveSpeed = distance / duration;
        controller.ballSpeed = effectiveSpeed;
        controller.DisablePhysics();

        float elapsed = 0f;
        while (elapsed < duration && ball != null && !controller.hasCollided)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            ball.transform.position = controller.CalculateBallPosition(t);

            if (t > 0.1f && t < 0.9f)
            {
                float nextT = Mathf.Min(1f, t + 0.05f);
                Vector3 lookDirection = controller.CalculateBallPosition(nextT) - ball.transform.position;
                if (lookDirection.sqrMagnitude > 0.0001f)
                    ball.transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ball == null || controller.hasCollided)
            yield break;

        ball.transform.position = controller.CalculateBallPosition(1f);
        controller.EnablePhysics();
        controller.ApplyFinalVelocity();
    }

    private IEnumerator OutcomeTimeout(BallController controller, float timeout)
    {
        yield return new WaitForSeconds(timeout);

        if (controller != null && controller == activeBall && !controller.OutcomeReported)
            controller.ReportTimeout();
    }

    internal void CancelBallMotion(Coroutine routine)
    {
        if (routine != null)
            StopCoroutine(routine);
    }

    internal void NotifyBallOutcome(BallController controller, bool hit)
    {
        if (controller == null || controller != activeBall || controller.OutcomeReported)
            return;

        controller.MarkOutcomeReported();

        if (ballCoroutine != null)
        {
            StopCoroutine(ballCoroutine);
            ballCoroutine = null;
        }

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        BallDirection direction = controller.Direction;
        activeBall = null;
        isLaunching = false;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        BallResolved?.Invoke(direction, hit);
    }

    public void CancelActiveBall()
    {
        if (activeBall == null)
            return;

        BallController controller = activeBall;
        NotifyBallOutcome(controller, false);

        if (controller != null)
            Destroy(controller.gameObject);
    }
}

public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    private Coroutine ballCoroutine;
    private BallLauncher owner;
    private bool outcomeReported;

    public bool hasReached;
    public bool hasCollided;
    public int targetCont;
    public Transform origin;
    public Transform currentTarget;
    public float currentHeight;
    public float currentHorizontalPeak;
    public float ballSpeed;

    public BallDirection Direction { get; private set; }
    public bool OutcomeReported => outcomeReported;

    private RealtimeLog log;

    public void Initialize(
        BallLauncher launcher,
        BallDirection direction,
        Transform launchOrigin,
        Transform target,
        float height,
        float horizontalPeak)
    {
        owner = launcher;
        Direction = direction;
        origin = launchOrigin;
        currentTarget = target;
        currentHeight = height;
        currentHorizontalPeak = horizontalPeak;
        rb = GetComponent<Rigidbody>();
        log = RealtimeLog.instance;
    }

    public void SetCoroutine(Coroutine routine)
    {
        ballCoroutine = routine;
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (log == null)
            log = RealtimeLog.instance;
    }

    private void Update()
    {
        if (log != null)
            log._ballPosition = transform.position;
    }

    public void DisablePhysics()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void EnablePhysics()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void ApplyFinalVelocity()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null || currentTarget == null)
            return;

        Vector3 direction = (currentTarget.position - transform.position).normalized;
        rb.linearVelocity = direction * ballSpeed;
    }

    public Vector3 CalculateBallPosition(float t)
    {
        // Interpolação reta entre lançador e alvo.
        Vector3 basePos = Vector3.Lerp(origin.position, currentTarget.position, t);

        // Única curvatura adicional: vertical no eixo Y global.
        // Assim a trajetória permanece em um plano ortogonal ao chão.
        float parabola = 4f * currentHeight * t * (1f - t);

        return basePos + Vector3.up * parabola;
    }

    internal void MarkOutcomeReported()
    {
        outcomeReported = true;
    }

    public void ReportTimeout()
    {
        if (outcomeReported)
            return;

        hasCollided = true;

        if (log != null)
            log._acerto = "ERROU";

        if (owner == null || owner.ShowLegacyOutcomeFeedback)
        {
            FeedbackController feedback = FeedbackController.instance;
            if (feedback != null)
                feedback.PlayErrou();
        }

        owner?.NotifyBallOutcome(this, false);
        StartCoroutine(DestroyDelay(1f));
    }

    private IEnumerator DestroyDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (outcomeReported || (!collision.gameObject.CompareTag("Ground") && !collision.gameObject.CompareTag("Player")))
            return;

        hasCollided = true;
        owner?.CancelBallMotion(ballCoroutine);

        if (log != null)
        {
            log._collisionTime = DateTime.Now.ToString("HH:mm:ss");
            log._acerto = collision.gameObject.CompareTag("Player") ? "ACERTOU" : "ERROU";
        }

        bool hit = collision.gameObject.CompareTag("Player");

        if (owner == null || owner.ShowLegacyOutcomeFeedback)
        {
            FeedbackController feedback = FeedbackController.instance;
            if (feedback != null)
            {
                if (hit)
                    feedback.PlayParabens();
                else
                    feedback.PlayErrou();
            }
        }

        owner?.NotifyBallOutcome(this, hit);
        EnablePhysics();
        StartCoroutine(DestroyDelay(5f));
    }
}
