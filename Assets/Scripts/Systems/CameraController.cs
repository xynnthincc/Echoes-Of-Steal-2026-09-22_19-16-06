using System.Collections;
using EchoesOfSteal.Combat;
using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Kamera gameplay: mengikuti pemain dengan smoothing + clamp ke batas arena,
    /// plus screen shake saat serangan mengenai musuh (FR-7, intensitas proporsional
    /// jumlah musuh kena). Berlangganan event AttackArea.OnAttackHit — event-driven.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform _target;
        [SerializeField, Range(1f, 20f)] private float _followSmoothing = 8f;
        [SerializeField, Min(1f)] private float _arenaHalfWidth = 20f;
        [SerializeField, Min(1f)] private float _arenaHalfHeight = 12f;

        [Header("Shake (FR-7)")]
        [SerializeField] private AttackArea _attackArea;
        [SerializeField, Min(0f)] private float _intensityPerTarget = 0.14f;
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float _maxIntensity = 0.45f;

        private Camera _camera;
        private Vector3 _shakeOffset;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (_attackArea != null)
                _attackArea.OnAttackHit += HandleAttackHit;
        }

        private void OnDisable()
        {
            if (_attackArea != null)
                _attackArea.OnAttackHit -= HandleAttackHit;
        }

        private void LateUpdate()
        {
            if (_target != null)
            {
                Vector3 desired = _target.position;
                float halfHeight = _camera.orthographicSize;
                float halfWidth = halfHeight * _camera.aspect;

                float clampX = Mathf.Max(0f, _arenaHalfWidth - halfWidth);
                float clampY = Mathf.Max(0f, _arenaHalfHeight - halfHeight);
                desired.x = Mathf.Clamp(desired.x, -clampX, clampX);
                desired.y = Mathf.Clamp(desired.y, -clampY, clampY);
                desired.z = transform.position.z;

                float t = 1f - Mathf.Exp(-_followSmoothing * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desired, t);
            }

            if (_shakeOffset != Vector3.zero)
                transform.position += _shakeOffset;
        }

        private void HandleAttackHit(int targetCount)
        {
            float intensity = Mathf.Min(_maxIntensity, _intensityPerTarget * targetCount);
            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(intensity));
        }

        private IEnumerator ShakeRoutine(float intensity)
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                float falloff = 1f - (elapsed / _shakeDuration);
                _shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * intensity * falloff,
                    Random.Range(-1f, 1f) * intensity * falloff,
                    0f);
                yield return null;
            }

            _shakeOffset = Vector3.zero;
        }
    }
}
