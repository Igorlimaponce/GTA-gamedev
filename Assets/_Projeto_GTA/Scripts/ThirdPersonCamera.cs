using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Câmera orbital de 3ª pessoa controlada pelo mouse.
    /// Segue um alvo (player ou carro), com colisão simples para não atravessar paredes.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Alvo")]
        [Tooltip("Objeto seguido pela câmera. Pode ser trocado em runtime (player <-> carro).")]
        public Transform target;
        [Tooltip("Deslocamento do ponto focal em relação ao alvo (altura do olhar).")]
        public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Distância")]
        public float distance = 5f;
        public float minDistance = 1.5f;

        [Header("Mouse")]
        public float mouseSensitivity = 3f;
        public float minPitch = -30f;
        public float maxPitch = 70f;
        [Tooltip("Inverter o eixo vertical do mouse.")]
        public bool invertY = false;

        [Header("Suavização")]
        public float followSmooth = 12f;

        [Header("Colisão")]
        public LayerMask collisionMask = ~0;
        public float collisionRadius = 0.25f;

        private float _yaw;
        private float _pitch = 15f;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        /// <summary>Troca o alvo seguido pela câmera (usado ao entrar/sair do carro).</summary>
        public void SetTarget(Transform newTarget, Vector3 offset)
        {
            target = newTarget;
            targetOffset = offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Só gira a câmera quando o jogo não está pausado (cursor travado).
            if (Time.timeScale > 0f)
            {
                _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
                _pitch += invertY ? mouseY : -mouseY;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focusPoint = target.position + targetOffset;

            // Posição desejada atrás do alvo.
            Vector3 desiredPos = focusPoint - rotation * Vector3.forward * distance;

            // Colisão: se houver parede entre o foco e a câmera, aproxima.
            if (Physics.SphereCast(focusPoint, collisionRadius,
                    (desiredPos - focusPoint).normalized, out RaycastHit hit,
                    distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float clamped = Mathf.Max(minDistance, hit.distance);
                desiredPos = focusPoint - rotation * Vector3.forward * clamped;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.unscaledDeltaTime);
            transform.rotation = rotation;
        }
    }
}
