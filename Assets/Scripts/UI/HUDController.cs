using EchoesOfSteal.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// HUD (FR-9): health bar kiri atas, score kanan atas, wave indicator atas.
    /// Update real-time murni via event dari GameManager/WaveSpawner — tanpa polling.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WaveSpawner _waveSpawner;
        [SerializeField] private Image _healthBarFill;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _waveText;

        private void OnEnable()
        {
            _gameManager.OnGameStarted += HandleGameStarted;
            _gameManager.OnScoreChanged += HandleScoreChanged;
            _gameManager.OnHealthChanged += HandleHealthChanged;
            _waveSpawner.OnWaveStarted += HandleWaveStarted;
        }

        private void OnDisable()
        {
            _gameManager.OnGameStarted -= HandleGameStarted;
            _gameManager.OnScoreChanged -= HandleScoreChanged;
            _gameManager.OnHealthChanged -= HandleHealthChanged;
            _waveSpawner.OnWaveStarted -= HandleWaveStarted;
        }

        private void HandleGameStarted()
        {
            if (_healthBarFill != null)
                _healthBarFill.fillAmount = 1f;
            HandleScoreChanged(0);
            if (_waveText != null)
                _waveText.SetText(string.Empty);
        }

        private void HandleScoreChanged(int score)
        {
            if (_scoreText != null)
                _scoreText.SetText("Score: {0}", score);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (_healthBarFill != null && max > 0f)
                _healthBarFill.fillAmount = current / max;
        }

        private void HandleWaveStarted(int waveNumber)
        {
            if (_waveText != null)
                _waveText.SetText("Wave {0}", waveNumber);
        }
    }
}
