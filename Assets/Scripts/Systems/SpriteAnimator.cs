using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Animator sprite frame-based yang ringan (tanpa Animator controller) untuk karakter pixel-art.
    /// Mendukung dua set frame (idle & walk) — dipilih via SetWalking() dari controller gerak.
    /// Pool-safe: tanpa state yang bocor antar instance.
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _idleFrames;
        [SerializeField] private Sprite[] _walkFrames;
        [SerializeField, Min(0.01f)] private float _secondsPerFrame = 0.15f;

        private float _timer;
        private int _index;
        private bool _walking;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>Mengganti set frame aktif (idle/walk) — reset ke frame pertama saat berubah.</summary>
        public void SetWalking(bool walking)
        {
            if (_walking == walking)
                return;

            _walking = walking;
            _index = 0;
            _timer = 0f;
        }

        private void Update()
        {
            Sprite[] frames = ActiveFrames;
            if (frames == null || frames.Length < 2 || _renderer == null)
                return;

            _timer += Time.deltaTime;
            if (_timer < _secondsPerFrame)
                return;

            _timer -= _secondsPerFrame;
            _index = (_index + 1) % frames.Length;
            _renderer.sprite = frames[_index];
        }

        private Sprite[] ActiveFrames
        {
            get
            {
                if (_walking && WalkValid)
                    return _walkFrames;
                if (IdleValid)
                    return _idleFrames;
                return _walkFrames;
            }
        }

        private bool WalkValid => _walkFrames != null && _walkFrames.Length > 0;
        private bool IdleValid => _idleFrames != null && _idleFrames.Length > 0;
    }
}
