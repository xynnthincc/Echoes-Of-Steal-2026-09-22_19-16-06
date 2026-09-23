using System.Collections;
using System;
using EchoesOfSteal.Enemy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Spawner wave musuh (FR-3): mengambil konfigurasi dari WaveData (ScriptableObject),
    /// instansiasi lewat ObjectPool (tanpa Instantiate/Destroy runtime), spawn di ring
    /// mengelilingi pemain. Mengekspos event (OnWaveStarted/OnWaveCleared/OnEnemyKilled)
    /// untuk UI & GameManager di fase berikutnya — tanpa referensi langsung antar sistem.
    /// Wave terakhir diulang terus untuk survival tanpa batas.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private EnemyAI _enemyPrefab;
        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform _playerTransform;

        [Header("Behavior")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField, Min(1f)] private float _spawnDistance = 9f;

        private ObjectPool<EnemyAI> _pool;
        private int _aliveCount;
        private Coroutine _waveRoutine;

        /// <summary>Argumen: nomor wave (1-based).</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>Argumen: nomor wave (1-based) yang baru selesai.</summary>
        public event Action<int> OnWaveCleared;

        /// <summary>Argumen: musuh yang dikalahkan (untuk skor di Fase 3).</summary>
        public event Action<EnemyAI> OnEnemyKilled;

        private void Awake()
        {
            if (_enemyPrefab == null || _waves == null || _waves.Length == 0 || _playerTransform == null)
            {
                Debug.LogError("WaveSpawner: _enemyPrefab, _waves, dan _playerTransform wajib di-assign.", this);
                enabled = false;
                return;
            }

            _pool = new ObjectPool<EnemyAI>(_enemyPrefab, transform);
            _pool.InstanceCreated += WireEnemy;
        }

        private void Start()
        {
            if (_autoStart)
                StartSpawning();
        }

        /// <summary>Memulai loop wave (dipanggil GameManager saat game start / restart).</summary>
        public void StartSpawning()
        {
            if (_waveRoutine != null)
                return;

            _aliveCount = 0;
            _waveRoutine = StartCoroutine(RunWaves());
        }

        /// <summary>Menghentikan loop wave (dipanggil GameManager saat Game Over).</summary>
        public void StopSpawning()
        {
            if (_waveRoutine == null)
                return;

            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        private IEnumerator RunWaves()
        {
            int waveNumber = 0;
            while (true)
            {
                WaveData wave = _waves[Mathf.Min(waveNumber, _waves.Length - 1)];
                waveNumber++;

                OnWaveStarted?.Invoke(waveNumber);
                yield return SpawnWave(wave);
                yield return new WaitUntil(() => _aliveCount == 0);
                OnWaveCleared?.Invoke(waveNumber);

                yield return new WaitForSeconds(wave.DelayAfterWave);
            }
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            for (int i = 0; i < wave.EnemyCount; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(wave.SpawnInterval);
            }
        }

        private void SpawnEnemy()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnDistance;
            Vector2 position = (Vector2)_playerTransform.position + offset;

            EnemyAI enemy = _pool.Get(position);
            enemy.SetTarget(_playerTransform);
            _aliveCount++;
        }

        private void WireEnemy(EnemyAI enemy)
        {
            enemy.Health.Died += () => HandleEnemyDeath(enemy);
        }

        private void HandleEnemyDeath(EnemyAI enemy)
        {
            _aliveCount--;
            OnEnemyKilled?.Invoke(enemy);
            _pool.Release(enemy);
        }
    }
}
