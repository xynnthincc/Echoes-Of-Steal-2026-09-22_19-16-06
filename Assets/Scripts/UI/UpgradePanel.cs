using System;
using System.Collections.Generic;
using EchoesOfSteal.Player;
using EchoesOfSteal.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// Panel level-up (pola Vampire Survivors): saat PlayerLevel.OnLevelUp, game dipause
    /// dan 3 kartu upgrade acak ditampilkan. Satu kartu dipilih → PlayerUpgrader.Apply → resume.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [Serializable]
        public class UpgradeCard
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text Title;
            public TMP_Text Description;
        }

        [SerializeField] private GameManager _gameManager;
        [SerializeField] private PlayerUpgrader _upgrader;
        [SerializeField] private PlayerLevel _playerLevel;
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _levelTitle;
        [SerializeField] private List<UpgradeCard> _cards = new List<UpgradeCard>();

        private void OnEnable()
        {
            if (_playerLevel != null)
                _playerLevel.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            if (_playerLevel != null)
                _playerLevel.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            Show(newLevel);
        }

        private void Show(int level)
        {
            IReadOnlyList<UpgradeDefinition> pool = _upgrader != null ? _upgrader.Pool : null;
            if (pool == null || pool.Count == 0 || _cards.Count == 0)
                return;

            List<UpgradeDefinition> choices = PickRandom(pool, _cards.Count);

            for (int i = 0; i < _cards.Count; i++)
            {
                UpgradeCard card = _cards[i];
                if (card == null || card.Button == null)
                    continue;

                if (i < choices.Count)
                {
                    UpgradeDefinition upgrade = choices[i];
                    if (card.Root != null)
                        card.Root.SetActive(true);
                    if (card.Title != null)
                        card.Title.SetText(upgrade.Title);
                    if (card.Description != null)
                        card.Description.SetText(upgrade.Description);

                    card.Button.onClick.RemoveAllListeners();
                    UpgradeDefinition captured = upgrade;
                    card.Button.onClick.AddListener(() => Choose(captured));
                }
                else if (card.Root != null)
                {
                    card.Root.SetActive(false);
                }
            }

            if (_levelTitle != null)
                _levelTitle.SetText("LEVEL {0}!", level);

            _content.SetActive(true);
            if (_gameManager != null)
                _gameManager.SetPaused(true);
        }

        private void Choose(UpgradeDefinition upgrade)
        {
            _upgrader.Apply(upgrade);
            _content.SetActive(false);
            if (_gameManager != null)
                _gameManager.SetPaused(false);
        }

        private static List<UpgradeDefinition> PickRandom(IReadOnlyList<UpgradeDefinition> pool, int count)
        {
            List<UpgradeDefinition> copy = new List<UpgradeDefinition>(pool);
            List<UpgradeDefinition> picked = new List<UpgradeDefinition>();
            while (picked.Count < count && copy.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, copy.Count);
                picked.Add(copy[index]);
                copy.RemoveAt(index);
            }
            return picked;
        }
    }
}
