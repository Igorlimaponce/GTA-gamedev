using UnityEngine;

namespace ProjetoGTA
{
    /// <summary>
    /// Gerenciador de música persistente entre cenas (singleton).
    /// Persiste do Menu ao Jogo e toca a trilha em loop.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        private AudioSource _source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            if (_source.clip != null && !_source.isPlaying)
                _source.Play();
        }

        public void Play()
        {
            if (!_source.isPlaying) _source.Play();
        }

        public void Stop() => _source.Stop();

        public void SetVolume(float v) => _source.volume = Mathf.Clamp01(v);
    }
}
