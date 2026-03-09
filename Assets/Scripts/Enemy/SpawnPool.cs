using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
//此脚本决定生成哪种敌人
[System.Serializable]
public class SpawnEntry
{
    public EnemyData enemyData;
    public int currentAlive; // 运行时计数，不在 Inspector 显示
}

public class SpawnPool : MonoBehaviour
{
   [Header("可生成的敌人列表（策划在此配置）")]
    [SerializeField] private List<EnemyData> enemyDataList = new();

    private List<SpawnEntry> entries = new();
    [SerializeField] private int totalAlive = 0;

    // 👇 存活敌人实例列表 —— 使用 EnemyController
    private List<EnemyController> aliveEnemies = new();

    // 👇 场上无敌人事件
    public UnityEvent OnAllEnemiesDefeated;

    public int TotalAlive => totalAlive;

    void Awake()
    {
        InitializeEntries();
        OnAllEnemiesDefeated ??= new UnityEvent();
    }

    void InitializeEntries()
    {
        entries.Clear();
        foreach (var data in enemyDataList)
        {
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                entries.Add(new SpawnEntry { enemyData = data, currentAlive = 0 });
            }
        }
    }

    public List<EnemyData> GetRegisteredEnemies() => new List<EnemyData>(enemyDataList);

    public EnemyData SelectRandom()
    {
        var candidates = new List<SpawnEntry>();
        var totalWeight = 0;

        foreach (var entry in entries)
        {
            if (entry.currentAlive < entry.enemyData.stats.maxAlive)
            {
                candidates.Add(entry);
                totalWeight += entry.enemyData.stats.spawnWeight;
            }
        }

        if (candidates.Count == 0 || totalWeight <= 0) return null;

        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidates[i].enemyData.stats.spawnWeight;
            if (rand < cumulative)
            {
                return candidates[i].enemyData;
            }
        }

        return candidates[^1].enemyData;
    }

    // ✅ 修正：参数类型为 EnemyController
    public void IncrementAlive(string enemyId, EnemyController enemyInstance)
    {
        if (enemyInstance == null) return;

        var entry = entries.Find(e => e.enemyData.id == enemyId);
        if (entry != null)
        {
            entry.currentAlive++;
            totalAlive++;
            aliveEnemies.Add(enemyInstance);
        }
    }

    // ✅ 修正：参数类型为 EnemyController
    public void DecrementAlive(string enemyId, EnemyController enemyInstance)
    {
        if (enemyInstance == null) return;

        var entry = entries.Find(e => e.enemyData.id == enemyId);
        if (entry != null && entry.currentAlive > 0)
        {
            entry.currentAlive--;
            totalAlive--;
            aliveEnemies.Remove(enemyInstance);

            if (totalAlive == 0)
            {
                OnAllEnemiesDefeated?.Invoke();
            }
        }
    }

    /// <summary>
    /// 立即杀死所有当前存活的敌人（用于波次结束、关卡重置等）
    /// </summary>
    public void KillAllEnemies()
    {
        // 防止在遍历时修改列表（先拷贝）
        var enemiesToKill = new List<EnemyController>(aliveEnemies);
    
        foreach (var enemy in enemiesToKill)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.ForceDie(); // 我们将新增这个方法
            }
        }
    }
    // ✅ 修正：返回 EnemyController 列表
    public IReadOnlyList<EnemyController> GetAliveEnemies() => aliveEnemies.AsReadOnly();
}
