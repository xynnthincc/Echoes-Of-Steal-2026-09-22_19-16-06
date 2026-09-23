using EchoesOfSteal.Combat;
using EchoesOfSteal.UI;
using UnityEngine;

namespace EchoesOfSteal.Player
{
    /// <summary>
    /// Menggerakkan pemain 360° via input joystick dan memicu serangan (FR-1, FR-2).
    /// Cache referensi physics di Awake; komunikasi ke sistem lain lewat event AttackArea.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Attack")]
        [SerializeField] private AttackArea _attackArea;
        [SerializeField] private float _attackCooldown = 0.4f;

        private Rigidbody2D _rb;
        private Vector2 _facing = Vector2.up;
        private float _nextAttackTime;

        /// <summary>Arah hadap terakhir pemain (dipakai arah hitbox serangan).</summary>
        public Vector2 Facing => _facing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            Vector2 input = _joystick != null ? _joystick.InputVector : Vector2.zero;
            _rb.linearVelocity = input * _moveSpeed;

            if (input.sqrMagnitude > 0.001f)
                _facing = input.normalized;
        }

        /// <summary>
        /// Memicu serangan sekali dengan arah hadap terakhir.
        /// Dipanggil dari UI Button Attack (OnClick).
        /// </summary>
        public void TryAttack()
        {
            if (Time.time < _nextAttackTime || _attackArea == null)
                return;

            _nextAttackTime = Time.time + _attackCooldown;
            _attackArea.PerformAttack(_facing);
        }
    }
}
