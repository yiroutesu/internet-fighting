// Assets/Scripts/Experience/ExperienceOrbSpawner.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 经验球生成器（带对象池），无需 ResourceManager
/// 挂在 GameServices 或任意常驻 GameObject 上
/// </summary>
public class ExperienceOrbSpawner : MonoBehaviour
{
    public static ExperienceOrbSpawner Instance { get; private set; }

    [Header("经验球 Prefab")]
    [Tooltip("拖入你的 ExperienceOrb.prefab")]
    public GameObject experienceOrbPrefab; // 👈 直接在这里引用！

    [Header("经验球设置")]
    public float orbLifetime = 60f; // 👈 新增：可在 Inspector 设置
    
    [Header("对象池设置")]
    public int initialPoolSize = 50;
    public bool allowDynamicExpansion = true;
    public Transform parent;
    private Queue<GameObject> _pool = new Queue<GameObject>();

    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (experienceOrbPrefab == null)
        {
            Debug.LogError("[ExperienceOrbSpawner] Missing ExperienceOrb Prefab!");
            enabled = false;
            return;
        }

        PreWarmPool();
    }

    private void PreWarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledOrb();
        }
        Debug.Log($"[ExperienceOrbSpawner] Pool initialized with {initialPoolSize} orbs.");
    }

    private GameObject CreatePooledOrb()
    {
        GameObject orb = Instantiate(experienceOrbPrefab, parent);
        orb.SetActive(false);

        var orbComp = orb.GetComponent<ExperienceOrb>();
        if (orbComp == null)
        {
            Debug.LogError("ExperienceOrb component missing on prefab!");
            Destroy(orb);
            return null;
        }

        // 注册回收回调
        orbComp.onCollected += () => ReturnToPool(orb);

        _pool.Enqueue(orb);
        return orb;
    }

    /// <summary>
    /// 生成一个经验球
    /// </summary>
    public void Spawn(Vector3 position, int experienceValue)
    {
        if (experienceValue <= 0 || experienceOrbPrefab == null) return;

        GameObject orbObj = null;

        if (_pool.Count > 0)
        {
            orbObj = _pool.Dequeue();
        }
        else if (allowDynamicExpansion)
        {
            Debug.LogWarning("[ExperienceOrbSpawner] Pool exhausted! Creating new orb dynamically.");
            orbObj = CreatePooledOrb(); // 创建后自动入队
            if (orbObj != null) _pool.Dequeue(); // 立即取出
        }
        else
        {
            Debug.LogWarning("Experience orb spawn denied: pool empty and expansion disabled.");
            return;
        }

        if (orbObj == null) return;

        orbObj.transform.position = position;
        orbObj.SetActive(true);

        var orb = orbObj.GetComponent<ExperienceOrb>();
        orb.ResetOrb(experienceValue, orbLifetime); // 👈 使用 spawner 的配置

        // 重新绑定回收回调（每次 Spawn 都新建委托，避免重复）
        orb.onCollected += () => ReturnToPool(orbObj);
    }

    private void ReturnToPool(GameObject orb)
    {
        if (orb != null)
        {
            _pool.Enqueue(orb);
        }
    }

    // 可选：调试用
    public int GetAvailableCount() => _pool.Count;
}