using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoGTA
{
    /// <summary>
    /// Controla os botões do menu principal e garante que a música toque.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            // Se o MusicManager já existe (vindo do jogo), manda tocar.
            MusicManager.Instance?.Play();
        }

        public void PlayGame()
        {
            SceneManager.LoadScene("GameScene");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
