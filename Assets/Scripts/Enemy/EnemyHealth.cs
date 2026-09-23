using System;
using EchoesOfSteal.Combat;
using UnityEngine;

namespace EchoesOfSteal.Enemy
{
    /// <summary>
    /// HP musuh + implementasi IDamageable (damage, knockback, hit-react).
    /// Saat HP habis memicu event Died — WaveSpawner yang mengembalikan instance ke pool.
    /// Reset HP otomatis di OnEnable (pola pool enable/disable, tanpa Instantiate/Destroy runtime).
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData _data;

        private EnemyAI _ai;
        private Rigidbody2D _rb;
        private float _currentHealth;
        private bool _isDead;

        /// <summary>Dipicu sekali saat HP habis. Instance tidak di-Destroy — dikelola pool.</summary>
        public event Action Died;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _currentHealth = _data.MaxHealth;
            _isDead = false;
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (_isDead)
                return;

            _currentHealth -= amount;
            _ai.NotifyDamaged();

            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                Died?.Invoke();
            }
        }
    }
}
