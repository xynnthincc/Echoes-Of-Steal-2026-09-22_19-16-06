using System;
using UnityEngine;

namespace EchoesOfSteal.Combat
{
    /// <summary>
    /// Hitbox serangan melee: deteksi target via OverlapBox (bukan collision event),
    /// terapkan damage lewat IDamageable dan impuls knockback ke Rigidbody2D target.
    /// Mengekspos event OnAttackHit untuk sistem lain (mis. screen shake) tanpa referensi langsung.
    /// </summary>
    public class AttackArea : MonoBehaviour
    {
        [SerializeField] private float _damage = 1f;
        [SerializeField] private float _knockbackForce = 8f;
        [SerializeField] private Vector2 _boxSize = new Vector2(1.2f, 1.2f);
        [SerializeField] private float _forwardOffset = 0.8f;
        [SerializeField] private LayerMask _targetLayers;

        private const int MaxHits = 32;
        private readonly Collider2D[] _results = new Collider2D[MaxHits];
        private ContactFilter2D _contactFilter;
        private Vector2 _lastDirection = Vector2.up;

        /// <summary>Dipanggil saat serangan mengenai target. Argumen: jumlah target yang kena.</summary>
        public event Action<int> OnAttackHit;

        /// <summary>
        /// Memicu hitbox sekali di depan karakter mengikuti attackDirection.
        /// </summary>
        /// <returns>Jumlah target yang terkena.</returns>
        public int PerformAttack(Vector2 attackDirection)
        {
            Vector2 direction = attackDirection.sqrMagnitude > 0.0001f
                ? attackDirection.normalized
                : Vector2.up;
            _lastDirection = direction;

            Vector2 center = (Vector2)transform.position + direction * _forwardOffset;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            _contactFilter.SetLayerMask(_targetLayers);
            int hitCount = Physics2D.OverlapBox(center, _boxSize, angle, _contactFilter, _results);

            int damagedCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _results[i];

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage, direction, _knockbackForce);
                    damagedCount++;
                }
                else
                {
                    Rigidbody2D targetRb = hit.attachedRigidbody;
                    if (targetRb != null)
                        targetRb.AddForce(direction * _knockbackForce, ForceMode2D.Impulse);
                }
            }

            if (damagedCount > 0)
                OnAttackHit?.Invoke(damagedCount);

            return damagedCount;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 direction = _lastDirection;
            Vector2 center = (Vector2)transform.position + direction * _forwardOffset;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _boxSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
