using System;
using UnityEngine;

namespace EchoesOfSteal.Systems
{
    /// <summary>
    /// Pool generik untuk entity yang sering di-spawn (musuh, VFX) — wajib menggantikan
    /// Instantiate/Destroy runtime (prinsip arsitektur #2). Membungkus
    /// UnityEngine.Pool.ObjectPool dengan pola enable/disable GameObject:
    /// OnEnable/OnDisable komponen menjadi reset-state otomatis.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly UnityEngine.Pool.ObjectPool<T> _pool;

        /// <summary>
        /// Dipanggil sekali per instance baru yang dibuat (bukan per Get) —
        /// gunakan untuk wiring event yang persist selama umur instance, mis. death → Release.
        /// </summary>
        public event Action<T> InstanceCreated;

        /// <param name="prefab">Prefab root yang punya komponen T.</param>
        /// <param name="parent">Parent untuk instance (rapi di hierarchy).</param>
        /// <param name="prewarmCount">Instance yang disiapkan di constructor.</param>
        public ObjectPool(T prefab, Transform parent, int prewarmCount = 0, int defaultCapacity = 10, int maxSize = 200)
        {
            _pool = new UnityEngine.Pool.ObjectPool<T>(
                () => CreateInstance(prefab, parent),
                instance => instance.gameObject.SetActive(true),
                instance => instance.gameObject.SetActive(false),
                instance => UnityEngine.Object.Destroy(instance.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);

            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = _pool.Get();
                _pool.Release(instance);
            }
        }

        /// <summary>Mengambil instance dari pool pada posisi tertentu.</summary>
        public T Get(Vector2 position)
        {
            T instance = _pool.Get();
            instance.transform.position = position;
            return instance;
        }

        /// <summary>Mengembalikan instance ke pool (dinonaktifkan, siap dipakai ulang).</summary>
        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        private T CreateInstance(T prefab, Transform parent)
        {
            T instance = UnityEngine.Object.Instantiate(prefab, parent);
            InstanceCreated?.Invoke(instance);
            return instance;
        }
    }
}
