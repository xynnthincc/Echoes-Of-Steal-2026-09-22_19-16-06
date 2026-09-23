using EchoesOfSteal.Combat;
using EchoesOfSteal.Systems;
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
        [SerializeField] private SlashEffect _slashEffect;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteAnimator _animator;

        private Rigidbody2D _rb;
        private Vector2 _facing = Vector2.up;
        private float _nextAttackTime;

        /// <summary>Arah hadap terakhir pemain (dipakai arah hitbox serangan).</summary>
        public Vector2 Facing => _facing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void FixedUpdate()
        {
            Vector2 input = _joystick != null ? _joystick.InputVector : Vector2.zero;
            _rb.linearVelocity = input * _moveSpeed;

            bool isMoving = input.sqrMagnitude > 0.001f;
            if (isMoving)
            {
                _facing = input.normalized;
                if (input.x != 0f && _spriteRenderer != null)
                    _spriteRenderer.flipX = input.x < 0f;
            }

            if (_animator != null)
                _animator.SetWalking(isMoving);
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
            if (_slashEffect != null)
                _slashEffect.Play(_facing);
        }

        /// <summary>Upgrade: menambah kecepatan gerak (dipakai PlayerUpgrader).</summary>
        public void AddMoveSpeed(float amount)
        {
            _moveSpeed += amount;
        }

        /// <summary>Upgrade: mengalikan cooldown serangan (nilai &lt; 1 = lebih cepat).</summary>
        public void MultiplyAttackCooldown(float multiplier)
        {
            _attackCooldown *= Mathf.Max(0.05f, multiplier);
        }
    }
}
