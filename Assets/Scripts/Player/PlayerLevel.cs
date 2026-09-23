using System;
using UnityEngine;

namespace EchoesOfSteal.Player
{
    /// <summary>
    /// Level & XP pemain (pola Vampire Survivors): XP dari orb yang dikumpulkan,
    /// kurva naik per level, dan event untuk HUD/UpgradePanel (event-driven).
    /// </summary>
    public class PlayerLevel : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _baseXpPerLevel = 5;
        [SerializeField, Min(0)] private int _xpGrowthPerLevel = 3;
        [SerializeField, Min(0.5f)] private float _magnetRadius = 1.6f;

        private int _level = 1;
        private int _xp;
        private int _xpToNext;

        /// <summary>Argumen: level sekarang, XP saat ini, XP yang dibutuhkan level ini.</summary>
        public event Action<int, int, int> OnXpChanged;

        /// <summary>Argumen: level baru (dipicu saat naik level — UpgradePanel mendengarkan ini).</summary>
        public event Action<int> OnLevelUp;

        public int Level => _level;
        public float MagnetRadius => _magnetRadius;

        private void OnEnable()
        {
            _level = 1;
            _xp = 0;
            _xpToNext = _baseXpPerLevel;
            OnXpChanged?.Invoke(_level, _xp, _xpToNext);
        }

        /// <summary>Menambah XP (dari orb); level naik bila cukup — bisa multi-level.</summary>
        public void GainXP(int amount)
        {
            if (amount <= 0)
                return;

            _xp += amount;
            while (_xp >= _xpToNext)
            {
                _xp -= _xpToNext;
                _level++;
                _xpToNext = _baseXpPerLevel + (_level - 1) * _xpGrowthPerLevel;
                OnLevelUp?.Invoke(_level);
            }

            OnXpChanged?.Invoke(_level, _xp, _xpToNext);
        }

        /// <summary>Memperluas radius magnet XP (dipakai PlayerUpgrader).</summary>
        public void AddMagnetRadius(float amount)
        {
            _magnetRadius += amount;
        }
    }
}
