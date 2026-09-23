using System.Collections.Generic;
using EchoesOfSteal.Combat;
using EchoesOfSteal.Systems;
using UnityEngine;

namespace EchoesOfSteal.Player
{
    /// <summary>
    /// Pusat penerapan upgrade level-up ke semua komponen stat pemain
    /// (pola service sederhana — UpgradePanel memanggil Apply, komponen target
    /// tetap tidak saling mengenal).
    /// </summary>
    public class PlayerUpgrader : MonoBehaviour
    {
        [SerializeField] private List<UpgradeDefinition> _pool = new List<UpgradeDefinition>();

        private PlayerController _controller;
        private AttackArea _attackArea;
        private PlayerHealth _health;
        private PlayerLevel _level;

        /// <summary>Daftar upgrade yang bisa muncul di kartu level-up.</summary>
        public IReadOnlyList<UpgradeDefinition> Pool => _pool;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _attackArea = GetComponentInChildren<AttackArea>();
            _health = GetComponent<PlayerHealth>();
            _level = GetComponent<PlayerLevel>();
        }

        /// <summary>Menerapkan satu upgrade ke stat pemain sesuai tipenya.</summary>
        public void Apply(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
                return;

            switch (upgrade.Type)
            {
                case UpgradeDefinition.StatType.MoveSpeed:
                    _controller.AddMoveSpeed(upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.Damage:
                    _attackArea.AddDamage(upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.AttackSpeed:
                    _controller.MultiplyAttackCooldown(1f - upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.Knockback:
                    _attackArea.AddKnockback(upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.MagnetRadius:
                    _level.AddMagnetRadius(upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.MaxHealth:
                    _health.AddMaxHealth(upgrade.Value);
                    break;
                case UpgradeDefinition.StatType.AttackSize:
                    _attackArea.MultiplyBoxSize(1f + upgrade.Value);
                    break;
            }
        }
    }
}
