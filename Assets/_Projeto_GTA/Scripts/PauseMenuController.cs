using UnityEngine;
using UnityEngine.UI;

namespace ProjetoGTA
{
    /// <summary>
    /// Botões do painel de pausa. Fia listeners em runtime.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private void Start()
        {
            WireButton("BtnResume", Resume);
            WireButton("BtnMenu", ReturnToMenu);
        }

        public void Resume()
        {
            GameManager.Instance?.ResumeGame();
        }

        public void ReturnToMenu()
        {
            GameManager.Instance?.ReturnToMenu();
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
            Debug.LogWarning($"[PauseMenu] Botão '{buttonName}' não encontrado.");
        }
    }
}
