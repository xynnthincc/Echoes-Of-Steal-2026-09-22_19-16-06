using System.Collections;
using EchoesOfSteal.Player;
using EchoesOfSteal.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// HUD (FR-9): health bar kiri atas, XP bar + level, score kanan atas, wave indicator atas
    /// dengan banner pop. Update real-time murni via event dari GameManager/WaveSpawner/PlayerLevel — tanpa polling.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WaveSpawner _waveSpawner;
        [SerializeField] private PlayerLevel _playerLevel;
        [SerializeField] private Image _healthBarFill;
        [SerializeField] private Image _xpBarFill;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField, Min(0.01f)] private float _bannerPopTime = 0.5f;
        [SerializeField, Min(1f)] private float _bannerPopScale = 1.35f;

        private Coroutine _bannerRoutine;

        private void OnEnable()
        {
            _gameManager.OnGameStarted += HandleGameStarted;
            _gameManager.OnScoreChanged += HandleScoreChanged;
            _gameManager.OnHealthChanged += HandleHealthChanged;
            _waveSpawner.OnWaveStarted += HandleWaveStarted;
            if (_playerLevel != null)
            {
                _playerLevel.OnXpChanged += HandleXpChanged;
                _playerLevel.OnLevelUp += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            _gameManager.OnGameStarted -= HandleGameStarted;
            _gameManager.OnScoreChanged -= HandleScoreChanged;
            _gameManager.OnHealthChanged -= HandleHealthChanged;
            _waveSpawner.OnWaveStarted -= HandleWaveStarted;
            if (_playerLevel != null)
            {
                _playerLevel.OnXpChanged -= HandleXpChanged;
                _playerLevel.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleGameStarted()
        {
            if (_healthBarFill != null)
                _healthBarFill.fillAmount = 1f;
            if (_xpBarFill != null)
                _xpBarFill.fillAmount = 0f;
            HandleScoreChanged(0);
            HandleLevelUp(1);
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

        private void HandleXpChanged(int level, int current, int needed)
        {
            if (_xpBarFill != null && needed > 0)
                _xpBarFill.fillAmount = Mathf.Clamp01((float)current / needed);
        }

        private void HandleLevelUp(int newLevel)
        {
            if (_levelText != null)
                _levelText.SetText("Lv {0}", newLevel);
        }

        private void HandleWaveStarted(int waveNumber)
        {
            if (_waveText == null)
                return;

            _waveText.SetText("Wave {0}", waveNumber);
            if (_bannerRoutine != null)
                StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(PopWaveBanner());
        }

        private IEnumerator PopWaveBanner()
        {
            Transform banner = _waveText.transform;
            float elapsed = 0f;

            while (elapsed < _bannerPopTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _bannerPopTime);
                float scale = Mathf.Lerp(_bannerPopScale, 1f, t);
                banner.localScale = Vector3.one * scale;
                yield return null;
            }

            banner.localScale = Vector3.one;
        }
    }
}
