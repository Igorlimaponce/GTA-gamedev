using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Lógica de entrar e sair do carro com a tecla E.
    /// Desativa o controle do player, ativa o CarController e reposiciona a câmera.
    /// </summary>
    public class VehicleEnterExit : MonoBehaviour
    {
        [Tooltip("Distância máxima do player para poder entrar no carro.")]
        public float enterRadius = 3f;
        [Tooltip("Onde o player reaparece ao sair do carro (em coordenadas locais do carro).")]
        public Vector3 exitOffset = new Vector3(-1.8f, 0.1f, 0f);

        private CarController _car;
        private ThirdPersonPlayerController _player;
        private ThirdPersonCamera _cam;
        private Transform _playerTransform;
        private bool _playerInCar;

        private void Awake()
        {
            _car = GetComponent<CarController>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.E)) return;

            if (_playerInCar)
            {
                ExitCar();
            }
            else
            {
                // Procura o player próximo ao carro.
                GameObject playerGO = GameObject.FindWithTag("Player");
                if (playerGO == null) return;

                float dist = Vector3.Distance(transform.position, playerGO.transform.position);
                if (dist > enterRadius) return;

                _player = playerGO.GetComponent<ThirdPersonPlayerController>();
                _cam = Camera.main?.GetComponent<ThirdPersonCamera>();
                _playerTransform = playerGO.transform;
                EnterCar();
            }
        }

        private void EnterCar()
        {
            if (_player == null) return;

            _player.SetControlEnabled(false);
            _playerTransform.SetParent(transform);
            _playerTransform.localPosition = new Vector3(0f, 0.2f, 0f);
            _playerTransform.gameObject.SetActive(false);  // oculta o modelo

            _car.SetControlEnabled(true);

            // Câmera passa a seguir o carro.
            if (_cam != null)
                _cam.SetTarget(transform, new Vector3(0f, 1.2f, 0f));

            _playerInCar = true;
            GameManager.Instance?.SetHint("[E] Sair do carro");
        }

        private void ExitCar()
        {
            _playerTransform.SetParent(null);
            _playerTransform.position = transform.TransformPoint(exitOffset);
            _playerTransform.gameObject.SetActive(true);

            _car.SetControlEnabled(false);

            _player.SetControlEnabled(true);

            // Câmera volta para o player.
            if (_cam != null)
                _cam.SetTarget(_playerTransform, new Vector3(0f, 1.6f, 0f));

            _playerInCar = false;
            GameManager.Instance?.SetHint("[E] Entrar no carro");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, enterRadius);
        }
    }
}
