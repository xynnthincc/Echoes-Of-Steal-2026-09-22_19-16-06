using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Konfigurasi satu wave (data-driven, FR-3): jumlah musuh, interval spawn,
    /// dan jeda setelah wave selesai. Array WaveData di WaveSpawner mendefinisikan eskalasi.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveData", menuName = "Echoes of Steal/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _enemyCount = 5;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 1f;
        [SerializeField, Min(0f)] private float _delayAfterWave = 3f;

        public int EnemyCount => _enemyCount;
        public float SpawnInterval => _spawnInterval;
        public float DelayAfterWave => _delayAfterWave;
    }
}
