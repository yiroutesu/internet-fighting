// BulletPool.cs
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [System.Serializable]
    public struct BulletType
    {
        public string key;
        public GameObject prefab;
        public int initialPoolSize;
    }

    public List<BulletType> bulletTypes = new List<BulletType>();
    private Dictionary<string, Queue<IBullet>> pools = new Dictionary<string, Queue<IBullet>>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var type in bulletTypes)
        {
            if (string.IsNullOrEmpty(type.key) || type.prefab == null) continue;
            if (!type.prefab.TryGetComponent(out IBullet _))
            {
                Debug.LogError($"Prefab '{type.prefab.name}' missing IBullet!");
                continue;
            }

            var pool = new Queue<IBullet>();
            for (int i = 0; i < type.initialPoolSize; i++)
            {
                GameObject obj = Instantiate(type.prefab, transform);
                obj.SetActive(false);
                if (obj.TryGetComponent(out BulletBase bb))
                {
                    bb.poolKey = type.key; // 注入 key
                }
                pool.Enqueue(obj.GetComponent<IBullet>());
            }
            pools[type.key] = pool;
        }
    }

    public IBullet GetBullet(string bulletKey)
    {
        if (!pools.TryGetValue(bulletKey, out var pool))
        {
            Debug.LogWarning($"Bullet type '{bulletKey}' not registered.");
            return null;
        }

        if (pool.Count == 0)
        {
            // 动态扩容
            var prefab = bulletTypes.Find(t => t.key == bulletKey).prefab;
            if (prefab != null)
            {
                GameObject newObj = Instantiate(prefab, transform);
                newObj.SetActive(false);
                if (newObj.TryGetComponent(out BulletBase bb))
                {
                    bb.poolKey = bulletKey;
                }
                var newBullet = newObj.GetComponent<IBullet>();
                pool.Enqueue(newBullet);
            }
        }

        return pool.Count > 0 ? pool.Dequeue() : null;
    }

    public void ReturnBullet(IBullet bullet)
    {
        if (bullet == null) return;

        string key = bullet.GetPoolKey();
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Bullet missing poolKey! Destroying.");
            if (bullet is Object obj && obj != null) Destroy(obj);
            return;
        }

        if (pools.TryGetValue(key, out var pool))
        {
            bullet.OnReturnToPool(); // 重置状态
            pool.Enqueue(bullet);
        }
        else
        {
            Debug.LogWarning($"No pool for key: {key}. Destroying.");
            if (bullet is Object obj && obj != null) Destroy(obj);
        }
    }

    public IBullet GetPlayerBullet(string bulletName) => GetBullet($"Player.{bulletName}");
    public IBullet GetEnemyBullet(string bulletName) => GetBullet($"Enemy.{bulletName}");
}