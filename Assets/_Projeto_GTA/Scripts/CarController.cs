using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Controlador de carro arcade via Rigidbody.
    /// Aceleração W/S, esterçamento A/D. Sem WheelColliders para máxima estabilidade.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Motor")]
        public float acceleration = 18f;
        public float maxSpeed = 22f;
        [Tooltip("Força de frenagem ao soltar o acelerador.")]
        public float dragForce = 8f;

        [Header("Esterçamento")]
        public float steerSpeed = 90f;

        [Header("Estabilidade")]
        [Tooltip("Drag linear aplicado ao Rigidbody quando controlado.")]
        public float linearDrag = 2f;
        [Tooltip("Drag angular para impedir rotação indesejada.")]
        public float angularDrag = 5f;

        private Rigidbody _rb;
        private bool _controlEnabled;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = new Vector3(0f, -0.4f, 0f); // baixa o CG para estabilidade
            _rb.angularDamping = angularDrag;
            _rb.linearDamping = 0f;
        }

        /// <summary>Liga ou desliga o controle do carro.</summary>
        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            _rb.linearDamping = enabled ? linearDrag : 1f;
        }

        private void FixedUpdate()
        {
            if (!_controlEnabled) return;

            float accelInput = Input.GetAxisRaw("Vertical");
            float steerInput = Input.GetAxisRaw("Horizontal");

            float currentSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);

            // Aplica força de aceleração apenas abaixo do limite.
            if (Mathf.Abs(currentSpeed) < maxSpeed)
                _rb.AddForce(transform.forward * accelInput * acceleration, ForceMode.Acceleration);

            // Drag ao soltar o acelerador.
            if (Mathf.Approximately(accelInput, 0f))
            {
                Vector3 forwardVel = transform.forward * currentSpeed;
                _rb.AddForce(-forwardVel * dragForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }

            // Esterça somente se estiver se movendo.
            if (Mathf.Abs(currentSpeed) > 0.5f)
            {
                float sign = Mathf.Sign(currentSpeed);
                float steerAngle = steerInput * steerSpeed * sign * Time.fixedDeltaTime;
                Quaternion steerRot = Quaternion.Euler(0f, steerAngle, 0f);
                _rb.MoveRotation(_rb.rotation * steerRot);
            }

            // Mantém o carro nivelado no eixo X/Z (anti-capotamento para baixa velocidade).
            if (_rb.linearVelocity.magnitude < 1f)
            {
                Vector3 euler = _rb.rotation.eulerAngles;
                _rb.MoveRotation(Quaternion.Euler(0f, euler.y, 0f));
            }
        }
    }
}
