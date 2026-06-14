using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Botões do painel de pausa — delega ao GameManager.
    /// Associe os botões "Continuar" e "Voltar ao Menu" do Canvas de pausa a estes métodos.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public void Resume()
        {
            GameManager.Instance?.ResumeGame();
        }

        public void ReturnToMenu()
        {
            GameManager.Instance?.ReturnToMenu();
        }
    }
}
