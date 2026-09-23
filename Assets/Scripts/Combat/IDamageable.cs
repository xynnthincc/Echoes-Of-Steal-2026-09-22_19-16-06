using UnityEngine;

namespace EchoesOfSteal.Combat
{
    /// <summary>
    /// Interface untuk entitas yang bisa menerima damage + knockback dari AttackArea.
    /// Damage dan knockback dikirim atomik dalam satu panggilan supaya implementor
    /// bisa mengatur hit-react (pause steering) dan impuls physics secara konsisten.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce);
    }
}
