using System.IO;
using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Salva um screenshot em PNG na área de trabalho ao pressionar F12.
    /// O arquivo aparece em ~/Desktop/GTA-Screenshots/.
    /// </summary>
    public class ScreenshotCapture : MonoBehaviour
    {
        [Tooltip("Escala do screenshot (1 = resolução atual, 2 = dobro).")]
        public int superSize = 1;

        private string _folder;

        private void Awake()
        {
            _folder = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                "GTA-Screenshots");
            Directory.CreateDirectory(_folder);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                Capture();
        }

        private void Capture()
        {
            string filename = $"GTA_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(_folder, filename);
            ScreenCapture.CaptureScreenshot(path, superSize);
            Debug.Log($"[Screenshot] Salvo em: {path}");
        }
    }
}
