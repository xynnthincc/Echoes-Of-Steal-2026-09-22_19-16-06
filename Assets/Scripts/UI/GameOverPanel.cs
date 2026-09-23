using EchoesOfSteal.Systems;
using TMPro;
using UnityEngine;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// Game Over screen (FR-8): menampilkan skor akhir + tombol Restart.
    /// Sama seperti StartPanel: komponen di parent yang selalu aktif,
    /// visual di child _content yang di-toggle saat event OnGameOver datang.
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _finalScoreText;

        private void OnEnable()
        {
            _content.SetActive(false);
            _gameManager.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            _gameManager.OnGameOver -= HandleGameOver;
        }

        /// <summary>Wired ke Button OnClick (tombol Restart).</summary>
        public void Restart()
        {
            _gameManager.Restart();
        }

        private void HandleGameOver(int finalScore)
        {
            if (_finalScoreText != null)
                _finalScoreText.SetText("Score: {0}", finalScore);
            _content.SetActive(true);
        }
    }
}
