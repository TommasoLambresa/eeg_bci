using UnityEngine;

// Controle contínuo da perna.
// inputValue pode ser atualizado por LSL ou UI a qualquer instante.
public class RaiseLeg : MonoBehaviour
{
    [Header("Environment Variables")]
    [Range(0f, 1f)] public float inputValue = 0f;
    public float minAngle = 65f;
    public float maxAngle = 0f;
    public float legForce = 300f;
    public float smoothVelocity;

    public float maxTorque = 50f;
    public float smoothTime = 0.1f;

    [Space, Header("Física da Perna")]
    public Transform _bone;
    public Rigidbody hipRb;

    [Space, Header("Diagnóstico")]
    public float currentAngle = 0f;
    [SerializeField] private float targetAngle = 65f;
    [SerializeField] private float appliedTorque = 0f;

    private void Update()
    {
        UpdateCurrentAngle();
    }

    private void FixedUpdate()
    {
        if (_bone == null || hipRb == null)
            return;

        UpdateCurrentAngle();

        float input = Mathf.Clamp01(inputValue);
        targetAngle = Mathf.Lerp(minAngle, maxAngle, input);

        // Controle proporcional simples e contínuo.
        // Também conduz a perna de volta ao repouso quando o input retorna a 0.
        appliedTorque =
            (targetAngle - currentAngle) *
            legForce *
            Time.fixedDeltaTime;

        appliedTorque = Mathf.Clamp(appliedTorque, -maxTorque, maxTorque);

        hipRb.AddTorque(
            _bone.right * appliedTorque,
            ForceMode.VelocityChange
        );
    }

    private void UpdateCurrentAngle()
    {
        if (_bone == null)
            return;

        currentAngle = _bone.localEulerAngles.x;

        if (currentAngle > 180f)
            currentAngle -= 360f;
    }
}