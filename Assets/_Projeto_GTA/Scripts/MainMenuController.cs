using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoGTA
{
    /// <summary>
    /// Controla os botões do menu principal.
    /// Fia os listeners em runtime para não depender de serialização do Editor.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            MusicManager.Instance?.Play();

            // Fia botões pelo nome — funciona independente de como o setup criou o Canvas.
            WireButton("BtnPlay", PlayGame);
            WireButton("BtnQuit", QuitGame);
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

        private void WireButton(string buttonName, UnityEngine.Events.UnityAction action)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn.name == buttonName)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(action);
                    return;
                }
            }
            Debug.LogWarning($"[MainMenu] Botão '{buttonName}' não encontrado.");
        }
    }
}
