using System;
using EchoesOfSteal.Enemy;
using EchoesOfSteal.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Alur game (FR-6, FR-8): Start → Playing → Game Over → Restart.
    /// Menerima event dari WaveSpawner & PlayerHealth (tidak query musuh langsung),
    /// menambah skor tiap musuh dikalahkan, dan membekukan waktu (Time.timeScale = 0)
    /// di luar state Playing supaya panel UI tetap interaktif.
    /// Restart lewat scene reload — state HP, skor, dan pool reset otomatis.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _waveSpawner;
        [SerializeField] private PlayerHealth _playerHealth;

        private int _score;
        private bool _isGameOver;

        /// <summary>Argumen: skor baru (FR-6, untuk HUD).</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Argumen: HP sekarang & maksimum (relay dari PlayerHealth untuk HUD).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Argumen: skor akhir saat Game Over (untuk panel Game Over).</summary>
        public event Action<int> OnGameOver;

        /// <summary>Dipicu saat gameplay dimulai (tombol Begin) — panel start menyembunyikan diri.</summary>
        public event Action OnGameStarted;

        private void Awake()
        {
            if (_waveSpawner == null || _playerHealth == null)
            {
                Debug.LogError("GameManager: _waveSpawner dan _playerHealth wajib di-assign.", this);
                enabled = false;
                return;
            }

            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            _waveSpawner.OnEnemyKilled += HandleEnemyKilled;
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _playerHealth.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            _waveSpawner.OnEnemyKilled -= HandleEnemyKilled;
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _playerHealth.OnPlayerDied -= HandlePlayerDied;
        }

        /// <summary>Relay event HP dari PlayerHealth ke HUD (harus method — bukan snapshot delegate event).</summary>
        private void HandleHealthChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        /// <summary>Memulai gameplay dari Start Screen (dipanggil tombol Begin).</summary>
        public void StartGame()
        {
            Time.timeScale = 1f;
            _score = 0;
            OnScoreChanged?.Invoke(_score);

            _waveSpawner.StartSpawning();
            OnGameStarted?.Invoke();
        }

        /// <summary>Restart run (dipanggil tombol Restart): reload scene aktif.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Pause/resume gameplay untuk UI pilihan (mis. UpgradePanel level-up).
        /// No-op setelah Game Over supaya panel tidak "membangkitkan" game yang sudah berakhir.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_isGameOver)
                return;

            Time.timeScale = paused ? 0f : 1f;
        }

        private void HandleEnemyKilled(EnemyAI enemy)
        {
            _score++;
            OnScoreChanged?.Invoke(_score);
        }

        private void HandlePlayerDied()
        {
            _isGameOver = true;
            Time.timeScale = 0f;
            _waveSpawner.StopSpawning();
            OnGameOver?.Invoke(_score);
        }
    }
}
