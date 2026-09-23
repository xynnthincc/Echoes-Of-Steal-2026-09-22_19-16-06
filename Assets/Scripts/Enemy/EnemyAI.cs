using EchoesOfSteal.Combat;
using UnityEngine;

namespace EchoesOfSteal.Enemy
{
    /// <summary>
    /// AI musuh: mengejar target (pemain) real-time (FR-4) dengan kecepatan dari EnemyData,
    /// plus damage kontak saat menempel pemain (FR-5) lewat IDamageable —
    /// i-frame ada di PlayerHealth, jadi musuh cukup memanggil TakeDamage tiap frame kontak.
    /// State sederhana (enum) untuk ekstensi tipe musuh berikutnya.
    /// Selama hit-react, steering dihentikan supaya impuls knockback tidak tertimpa velocity chase.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        private enum State { Idle, Chasing }

        [SerializeField] private EnemyData _data;
        [SerializeField] private LayerMask _playerLayers;

        private Rigidbody2D _rb;
        private EnemyHealth _health;
        private Transform _target;
        private State _state = State.Idle;
        private float _hitReactUntil = -1f;
        private IDamageable _touchTarget;

        /// <summary>EnemyHealth di GameObject yang sama (cache di Awake).</summary>
        public EnemyHealth Health => _health;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<EnemyHealth>();

            if (_data == null)
                Debug.LogError("EnemyAI: _data wajib di-assign.", this);
            if (_playerLayers.value == 0)
                Debug.LogWarning("EnemyAI: _playerLayers kosong — damage kontak ke pemain tidak aktif.", this);
        }

        /// <summary>Menetapkan target yang dikejar (dipanggil WaveSpawner setiap spawn dari pool).</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _state = target != null ? State.Chasing : State.Idle;
        }

        /// <summary>Menghentikan steering sesaat (durasi dari EnemyData) setelah terkena serangan.</summary>
        public void NotifyDamaged()
        {
            _hitReactUntil = Time.time + _data.HitReactDuration;
        }

        private void OnDisable()
        {
            _touchTarget = null;
        }

        private void FixedUpdate()
        {
            if (_state != State.Chasing || _target == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (Time.time < _hitReactUntil)
                return;

            Vector2 direction = ((Vector2)(_target.position - transform.position)).normalized;
            _rb.linearVelocity = direction * _data.MoveSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_touchTarget != null || (_playerLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (other.TryGetComponent(out IDamageable damageable))
                _touchTarget = damageable;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_touchTarget == null || _target == null)
                return;

            Vector2 knockbackDirection = ((Vector2)(_target.position - transform.position)).normalized;
            _touchTarget.TakeDamage(_data.ContactDamage, knockbackDirection, _data.ContactKnockback);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _touchTarget = null;
        }
    }
}
