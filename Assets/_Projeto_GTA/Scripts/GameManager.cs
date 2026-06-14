using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoGTA
{
    /// <summary>
    /// Gerencia pausa, HUD de dica e cursor. Singleton de cena.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI")]
        public Text hintText;
        public GameObject pausePanel;

        private bool _paused;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LockCursor(true);
        }

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            SetHint("[E] Entrar no carro");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        public void SetHint(string msg)
        {
            if (hintText != null) hintText.text = msg;
        }

        public void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(_paused);
            LockCursor(!_paused);
        }

        public void ResumeGame()
        {
            if (_paused) TogglePause();
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
