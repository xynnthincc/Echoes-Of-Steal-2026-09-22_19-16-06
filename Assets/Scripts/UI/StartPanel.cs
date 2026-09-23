using EchoesOfSteal.Systems;
using UnityEngine;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// Start screen (FR-8). Komponen ini menempel pada GameObject yang selalu aktif
    /// (parent), sementara visual panel ada di child _content yang di-toggle —
    /// supaya subscription event tetap hidup meski panel disembunyikan.
    /// </summary>
    public class StartPanel : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GameObject _content;

        private void OnEnable()
        {
            _content.SetActive(true);
            _gameManager.OnGameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            _gameManager.OnGameStarted -= HandleGameStarted;
        }

        /// <summary>Wired ke Button OnClick (tombol Begin).</summary>
        public void BeginGame()
        {
            _gameManager.StartGame();
        }

        private void HandleGameStarted()
        {
            _content.SetActive(false);
        }
    }
}
