using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Definisi satu upgrade level-up (data-driven, terinspirasi Vampire Survivors).
    /// Value diterapkan PlayerUpgrader sesuai _type — semantic tiap tipe dijelaskan di _description.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "Echoes of Steal/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public enum StatType { MoveSpeed, Damage, AttackSpeed, Knockback, MagnetRadius, MaxHealth, AttackSize }

        [SerializeField] private StatType _type;
        [SerializeField, Min(0.01f)] private float _value = 1f;
        [SerializeField] private string _title = "Upgrade";
        [Multiline]
        [SerializeField] private string _description = "Deskripsi efek.";

        public StatType Type => _type;
        public float Value => _value;
        public string Title => _title;
        public string Description => _description;
    }
}
