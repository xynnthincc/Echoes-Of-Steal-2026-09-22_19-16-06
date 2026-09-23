using System;
using System.Collections;
using EchoesOfSteal.Combat;
using EchoesOfSteal.Systems;
using UnityEngine;

namespace EchoesOfSteal.Enemy
{
    /// <summary>
    /// HP musuh + implementasi IDamageable (damage, knockback, hit-react, hit-flash).
    /// Saat HP habis memicu event Died — WaveSpawner yang mengembalikan instance ke pool.
    /// Reset HP & warna sprite otomatis di OnEnable (pola pool enable/disable).
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData _data;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color _flashColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField, Min(0f)] private float _flashDuration = 0.09f;

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
            if (_sprite == null)
                _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _currentHealth = _data.MaxHealth;
            _isDead = false;
            if (_sprite != null)
                _sprite.color = Color.white;
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (_isDead)
                return;

            _currentHealth -= amount;
            _ai.NotifyDamaged();

            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

            if (_flashDuration > 0f && _sprite != null)
                StartCoroutine(FlashRoutine());

            if (FeedbackSpawner.Instance != null)
                FeedbackSpawner.Instance.ShowDamage(transform.position, amount);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                if (FeedbackSpawner.Instance != null && _data != null)
                    FeedbackSpawner.Instance.SpawnXpOrb(transform.position, _data.XpReward);
                Died?.Invoke();
            }
        }

        private IEnumerator FlashRoutine()
        {
            _sprite.color = _flashColor;
            yield return new WaitForSeconds(_flashDuration);
            _sprite.color = Color.white;
        }
    }
}
