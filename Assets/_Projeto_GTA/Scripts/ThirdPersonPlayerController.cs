using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Controle de personagem em 3ª pessoa baseado em Rigidbody.
    /// Movimento WASD relativo à câmera, corrida com Shift e pulo com Espaço.
    /// Compatível com o Input legado (Project Settings -> Active Input Handling = "Both").
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ThirdPersonPlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [Tooltip("Velocidade de caminhada (m/s).")]
        public float walkSpeed = 4f;
        [Tooltip("Velocidade ao segurar Shift (m/s).")]
        public float runSpeed = 7f;
        [Tooltip("Velocidade da rotação do personagem para a direção do movimento.")]
        public float rotationSpeed = 12f;

        [Header("Pulo / Chão")]
        public float jumpForce = 5f;
        [Tooltip("Distância do teste de chão a partir da base do personagem.")]
        public float groundCheckDistance = 0.25f;
        public LayerMask groundMask = ~0;

        [Header("Câmera")]
        [Tooltip("Câmera usada como referência de direção. Se vazio, usa Camera.main.")]
        public Transform cameraTransform;

        private Rigidbody _rb;
        private Animator _animator;
        private CapsuleCollider _capsule;
        private bool _isGrounded;
        private bool _hasSpeedParam;
        private bool _hasGroundedParam;
        private bool _controlEnabled = true;

        // Hashes dos parâmetros de Animator (evita alocação por frame).
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;          // a rotação é controlada por código
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _capsule = GetComponent<CapsuleCollider>();
            // O Animator costuma estar no modelo filho (Synty).
            _animator = GetComponentInChildren<Animator>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            CacheAnimatorParams();
        }

        private void CacheAnimatorParams()
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == SpeedHash) _hasSpeedParam = true;
                if (p.nameHash == GroundedHash) _hasGroundedParam = true;
            }
        }

        /// <summary>Liga/desliga o controle (usado ao entrar/sair do carro).</summary>
        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            if (!enabled && _rb != null)
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            if (_hasSpeedParam && _animator != null)
                _animator.SetFloat(SpeedHash, 0f);
        }

        private void Update()
        {
            // Pulo é capturado no Update (mais responsivo) e aplicado no FixedUpdate.
            if (_controlEnabled && _isGrounded && Input.GetKeyDown(KeyCode.Space))
                _jumpQueued = true;
        }

        private bool _jumpQueued;

        private void FixedUpdate()
        {
            _isGrounded = CheckGrounded();

            if (!_controlEnabled)
            {
                if (_hasGroundedParam && _animator != null) _animator.SetBool(GroundedHash, _isGrounded);
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Direção relativa à câmera, projetada no plano do chão.
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (cameraTransform != null)
            {
                camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
                camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            }

            Vector3 moveDir = (camForward * v + camRight * h);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float targetSpeed = running ? runSpeed : walkSpeed;

            Vector3 horizontalVel = moveDir * targetSpeed;
            // Preserva a velocidade vertical (gravidade/pulo).
            _rb.linearVelocity = new Vector3(horizontalVel.x, _rb.linearVelocity.y, horizontalVel.z);

            // Rotaciona o personagem suavemente para a direção do movimento.
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
            }

            if (_jumpQueued)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                _jumpQueued = false;
            }

            UpdateAnimator(moveDir.magnitude * targetSpeed);
        }

        private void UpdateAnimator(float planarSpeed)
        {
            if (_animator == null) return;
            // Speed normalizado 0-1: 0=parado, 0.5=caminhando, 1=correndo
            float normalized = Mathf.Clamp01(planarSpeed / runSpeed);
            if (_hasSpeedParam) _animator.SetFloat(SpeedHash, normalized, 0.1f, Time.fixedDeltaTime);
            if (_hasGroundedParam) _animator.SetBool(GroundedHash, _isGrounded);
        }

        private bool CheckGrounded()
        {
            // Origem um pouco acima da base para evitar começar dentro do chão.
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float radius = _capsule != null ? Mathf.Max(0.1f, _capsule.radius * 0.9f) : 0.25f;
            return Physics.SphereCast(origin, radius, Vector3.down,
                out _, 0.1f + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.25f);
        }
    }
}
