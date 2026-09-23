using System.Collections;
using System;
using System.Collections.Generic;
using EchoesOfSteal.Enemy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Spawner wave musuh (FR-3): konfigurasi dari WaveData (ScriptableObject), instansiasi
    /// via ObjectPool per-tipe prefab (tanpa Instantiate/Destroy runtime), spawn di ring
    /// mengelilingi pemain. Komposisi tipe musuh bergantung nomor wave:
    /// index 0 = normal (selalu), 1 = fast (mulai wave 3), 2 = tank (mulai wave 5).
    /// Mengekspos event untuk UI & GameManager — tanpa referensi langsung antar sistem.
    /// Wave terakhir diulang terus untuk survival tanpa batas.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private EnemyAI[] _enemyPrefabs;
        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform _playerTransform;

        [Header("Behavior")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField, Min(1f)] private float _spawnDistance = 9f;
        [SerializeField, Min(1)] private int _fastFromWave = 3;
        [SerializeField, Min(1)] private int _tankFromWave = 5;

        private readonly Dictionary<EnemyAI, ObjectPool<EnemyAI>> _pools = new Dictionary<EnemyAI, ObjectPool<EnemyAI>>();
        private readonly Dictionary<EnemyAI, ObjectPool<EnemyAI>> _instancePool = new Dictionary<EnemyAI, ObjectPool<EnemyAI>>();
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
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0 || _waves == null || _waves.Length == 0 || _playerTransform == null)
            {
                Debug.LogError("WaveSpawner: _enemyPrefabs, _waves, dan _playerTransform wajib di-assign.", this);
                enabled = false;
                return;
            }

            foreach (EnemyAI prefab in _enemyPrefabs)
            {
                if (prefab == null || _pools.ContainsKey(prefab))
                    continue;

                ObjectPool<EnemyAI> pool = new ObjectPool<EnemyAI>(prefab, transform);
                pool.InstanceCreated += WireEnemy;
                _pools[prefab] = pool;
            }

            if (_pools.Count == 0)
            {
                Debug.LogError("WaveSpawner: tidak ada prefab musuh valid.", this);
                enabled = false;
            }
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
                yield return SpawnWave(wave, waveNumber);
                yield return new WaitUntil(() => _aliveCount == 0);
                OnWaveCleared?.Invoke(waveNumber);

                yield return new WaitForSeconds(wave.DelayAfterWave);
            }
        }

        private IEnumerator SpawnWave(WaveData wave, int waveNumber)
        {
            for (int i = 0; i < wave.EnemyCount; i++)
            {
                SpawnEnemy(waveNumber);
                yield return new WaitForSeconds(wave.SpawnInterval);
            }
        }

        private void SpawnEnemy(int waveNumber)
        {
            EnemyAI prefab = PickPrefab(waveNumber);
            if (prefab == null)
                return;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnDistance;
            Vector2 position = (Vector2)_playerTransform.position + offset;

            ObjectPool<EnemyAI> pool = _pools[prefab];
            EnemyAI enemy = pool.Get(position);
            _instancePool[enemy] = pool;
            enemy.SetTarget(_playerTransform);
            _aliveCount++;
        }

        private EnemyAI PickPrefab(int waveNumber)
        {
            int weights = 10;
            int fastWeight = waveNumber >= _fastFromWave && _enemyPrefabs.Length > 1 ? 5 : 0;
            int tankWeight = waveNumber >= _tankFromWave && _enemyPrefabs.Length > 2 ? 3 : 0;
            int total = weights + fastWeight + tankWeight;

            int roll = Random.Range(0, total);
            if (roll < weights)
                return _enemyPrefabs[0];
            if (roll < weights + fastWeight)
                return _enemyPrefabs[1];
            return _enemyPrefabs.Length > 2 ? _enemyPrefabs[2] : _enemyPrefabs[0];
        }

        private void WireEnemy(EnemyAI enemy)
        {
            enemy.Health.Died += () => HandleEnemyDeath(enemy);
        }

        private void HandleEnemyDeath(EnemyAI enemy)
        {
            _aliveCount--;
            OnEnemyKilled?.Invoke(enemy);

            if (_instancePool.TryGetValue(enemy, out ObjectPool<EnemyAI> pool))
            {
                _instancePool.Remove(enemy);
                pool.Release(enemy);
            }
        }
    }
}
