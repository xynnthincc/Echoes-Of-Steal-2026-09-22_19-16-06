using System.Collections;
using System.Collections.Generic;
using EchoesOfSteal.Systems;
using UnityEngine;

namespace EchoesOfSteal.Combat
{
    /// <summary>
    /// VFX tebasan (frame CircularSlash) berbasis object pool — tanpa Instantiate/Destroy runtime.
    /// Dipanggil PlayerController setiap serangan; frame dianimasikan via coroutine lalu
    /// instance dikembalikan ke pool.
    /// </summary>
    public class SlashEffect : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(0.01f)] private float _secondsPerFrame = 0.05f;
        [SerializeField, Min(0f)] private float _forwardOffset = 0.9f;
        [SerializeField, Min(0.1f)] private float _visualScale = 1.5f;
        [SerializeField, Min(1)] private int _poolSize = 3;

        private ObjectPool<SpriteRenderer> _pool;
        private readonly Dictionary<SpriteRenderer, Coroutine> _active = new Dictionary<SpriteRenderer, Coroutine>();

        private void Awake()
        {
            GameObject template = new GameObject("SlashInstance");
            template.transform.SetParent(transform, false);
            SpriteRenderer templateRenderer = template.AddComponent<SpriteRenderer>();
            templateRenderer.sortingOrder = 20;
            template.SetActive(false);

            _pool = new ObjectPool<SpriteRenderer>(templateRenderer, transform, _poolSize, _poolSize, _poolSize);
        }

        /// <summary>Memutar satu efek tebasan di depan pemain mengikuti arah serangan.</summary>
        public void Play(Vector2 direction)
        {
            if (_frames == null || _frames.Length == 0)
                return;

            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            Vector3 position = transform.position + (Vector3)(dir * _forwardOffset);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            SpriteRenderer instance = _pool.Get(position);
            instance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            instance.transform.localScale = Vector3.one * _visualScale;
            instance.sprite = _frames[0];

            if (_active.TryGetValue(instance, out Coroutine running) && running != null)
                StopCoroutine(running);
            _active[instance] = StartCoroutine(Animate(instance));
        }

        private IEnumerator Animate(SpriteRenderer instance)
        {
            for (int i = 0; i < _frames.Length; i++)
            {
                instance.sprite = _frames[i];
                yield return new WaitForSeconds(_secondsPerFrame);
            }

            _active.Remove(instance);
            _pool.Release(instance);
        }
    }
}
