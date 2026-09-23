using UnityEngine;

namespace EchoesOfSteal.Enemy
{
    /// <summary>
    /// Stats desain musuh (data-driven — ubah balancing tanpa sentuh kode).
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Echoes of Steal/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Stats")]
        [SerializeField, Min(1f)] private float _maxHealth = 3f;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
        [SerializeField, Min(0f)] private float _contactDamage = 1f;
        [SerializeField, Min(0f)] private float _contactKnockback = 5f;
        [SerializeField, Min(1f)] private float _xpReward = 2f;

        [Header("Hit Feedback")]
        [SerializeField, Min(0f)] private float _hitReactDuration = 0.2f;

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float ContactDamage => _contactDamage;
        public float ContactKnockback => _contactKnockback;
        public float XpReward => _xpReward;
        public float HitReactDuration => _hitReactDuration;
    }
}
