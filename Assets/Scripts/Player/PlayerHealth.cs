using System;
using EchoesOfSteal.Combat;
using UnityEngine;

namespace EchoesOfSteal.Player
{
    /// <summary>
    /// HP pemain (FR-5): menerima damage kontak musuh via IDamageable.
    /// Punya i-frame (cooldown damage) supaya kontak beruntun dari beberapa musuh
    /// tidak menguras HP sekaligus. Memicu OnPlayerDied saat HP habis —
    /// GameManager yang menangani Game Over, bukan komponen ini.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float _maxHealth = 10f;
        [SerializeField, Min(0f)] private float _damageCooldown = 0.75f;

        private Rigidbody2D _rb;
        private float _currentHealth;
        private float _nextDamageAllowedTime;

        /// <summary>Argumen: HP sekarang & maksimum (untuk HUD health bar).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Dipicu sekali saat HP habis.</summary>
        public event Action OnPlayerDied;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (Time.time < _nextDamageAllowedTime || _currentHealth <= 0f)
                return;

            _nextDamageAllowedTime = Time.time + _damageCooldown;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (knockbackForce > 0f)
                _rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

            if (_currentHealth <= 0f)
                OnPlayerDied?.Invoke();
        }
    }
}
