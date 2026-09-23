using System.Collections;
using System.Collections.Generic;
using EchoesOfSteal.Player;
using TMPro;
using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Pusat feedback gameplay: damage numbers (world-space TMP, pooled) dan XP orb
    /// (pooled, magnet ke pemain lalu collect). Instance statis sederhana ala GameManager —
    /// dipanggil EnemyHealth tanpa referensi silang antar sistem. Tanpa Instantiate/Destroy runtime.
    /// </summary>
    public class FeedbackSpawner : MonoBehaviour
    {
        [SerializeField] private Sprite _orbSprite;
        [SerializeField] private Color _orbColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField, Min(0.01f)] private float _textLifetime = 0.6f;
        [SerializeField, Min(0.1f)] private float _textRiseSpeed = 1.3f;
        [SerializeField, Min(0.1f)] private float _orbMagnetSpeed = 10f;
        [SerializeField, Min(0.05f)] private float _orbCollectDistance = 0.35f;
        [SerializeField, Min(0f)] private float _orbBaseScale = 0.5f;

        /// <summary>Instance aktif di scene (null-safe dipanggil dari sistem lain).</summary>
        public static FeedbackSpawner Instance { get; private set; }

        private class OrbState
        {
            public SpriteRenderer Renderer;
            public float Value;
        }

        private readonly List<OrbState> _orbs = new List<OrbState>();
        private ObjectPool<TextMeshPro> _textPool;
        private ObjectPool<SpriteRenderer> _orbPool;
        private Transform _player;
        private PlayerLevel _playerLevel;

        private void Awake()
        {
            Instance = this;

            GameObject textTemplate = new GameObject("DamageTextTemplate");
            textTemplate.transform.SetParent(transform, false);
            TextMeshPro text = textTemplate.AddComponent<TextMeshPro>();
            text.fontSize = 0.45f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            _textPool = new ObjectPool<TextMeshPro>(text, transform, 8, 16, 64);

            GameObject orbTemplate = new GameObject("XpOrbTemplate");
            orbTemplate.transform.SetParent(transform, false);
            SpriteRenderer orbRenderer = orbTemplate.AddComponent<SpriteRenderer>();
            orbRenderer.sprite = _orbSprite;
            orbRenderer.color = _orbColor;
            orbRenderer.sortingOrder = 15;
            orbTemplate.SetActive(false);
            _orbPool = new ObjectPool<SpriteRenderer>(orbRenderer, transform, 16, 32, 128);
        }

        private void Start()
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                _player = player.transform;
                _playerLevel = player.GetComponent<PlayerLevel>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Menampilkan angka damage melayang di posisi world (di-call EnemyHealth).</summary>
        public void ShowDamage(Vector3 position, float amount)
        {
            if (_textPool == null)
                return;

            TextMeshPro text = _textPool.Get(position + new Vector3(Random.Range(-0.15f, 0.15f), 0.25f, 0f));
            text.SetText("{0:0}", amount);
            StartCoroutine(RiseAndRelease(text));
        }

        /// <summary>Menjatuhkan XP orb di posisi (di-call EnemyHealth saat mati).</summary>
        public void SpawnXpOrb(Vector3 position, float value)
        {
            if (_orbPool == null || value <= 0f)
                return;

            SpriteRenderer orb = _orbPool.Get(position);
            orb.transform.localScale = Vector3.one * _orbBaseScale;
            _orbs.Add(new OrbState { Renderer = orb, Value = value });
        }

        private IEnumerator RiseAndRelease(TextMeshPro text)
        {
            float elapsed = 0f;
            Color startColor = text.color;
            startColor.a = 1f;
            text.color = startColor;

            while (elapsed < _textLifetime)
            {
                elapsed += Time.deltaTime;
                text.transform.position += Vector3.up * (_textRiseSpeed * Time.deltaTime);
                Color color = text.color;
                color.a = Mathf.Clamp01(1f - (elapsed / _textLifetime));
                text.color = color;
                yield return null;
            }

            _textPool.Release(text);
        }

        private void Update()
        {
            if (_player == null || _orbs.Count == 0)
                return;

            float magnetRadius = _playerLevel != null ? _playerLevel.MagnetRadius : 1.5f;
            Vector3 playerPosition = _player.position;

            for (int i = _orbs.Count - 1; i >= 0; i--)
            {
                OrbState orb = _orbs[i];
                if (orb.Renderer == null || !orb.Renderer.gameObject.activeSelf)
                {
                    _orbs.RemoveAt(i);
                    continue;
                }

                Vector3 toPlayer = playerPosition - orb.Renderer.transform.position;
                float distance = toPlayer.magnitude;

                if (distance <= magnetRadius)
                {
                    orb.Renderer.transform.position += toPlayer.normalized * (_orbMagnetSpeed * Time.deltaTime);
                    orb.Renderer.transform.localScale = Vector3.one * (_orbBaseScale * (1f + 0.25f * Mathf.PingPong(Time.time * 6f, 1f)));

                    if (distance <= _orbCollectDistance)
                    {
                        if (_playerLevel != null)
                            _playerLevel.GainXP(Mathf.Max(1, Mathf.RoundToInt(orb.Value)));
                        _orbPool.Release(orb.Renderer);
                        _orbs.RemoveAt(i);
                    }
                }
            }
        }
    }
}
