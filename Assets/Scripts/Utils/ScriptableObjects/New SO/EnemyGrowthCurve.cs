// EnemyGrowthCurve.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Enemies/Enemy Growth Curve")]
public class EnemyGrowthCurve : ScriptableObject
{
    // ========== 敌人属性成长 ==========
    public AnimationCurve health = new(new Keyframe(0, 1), new Keyframe(10, 2), new Keyframe(30, 8));
    public AnimationCurve damage = new(new Keyframe(0, 1), new Keyframe(10, 1.5f), new Keyframe(30, 4));
    public AnimationCurve speed = new(new Keyframe(0, 1), new Keyframe(30, 1.4f));
    public AnimationCurve defense = new(new Keyframe(0, 1), new Keyframe(30, 2));
    public AnimationCurve damageReduction = new(new Keyframe(0, 1), new Keyframe(30, 1.5f));
    [Range(0, 1)] public float maxDamageReduction = 0.75f;

    // ========== 生成行为成长（新增）==========
    [Header("生成行为成长")]
    [Tooltip("每次生成的敌人数量（批次大小）")]
    public AnimationCurve spawnBatchSize = new AnimationCurve(
        new Keyframe(1, 2),
        new Keyframe(5, 3),
        new Keyframe(10, 5),
        new Keyframe(20, 8)
    );

    [Tooltip("生成间隔（秒），值越小刷得越快")]
    public AnimationCurve spawnInterval = new AnimationCurve(
        new Keyframe(1, 3.0f),
        new Keyframe(5, 2.0f),
        new Keyframe(10, 1.2f),
        new Keyframe(20, 0.6f)
    );

    [Space]
    public int minBatchSize = 1;
    public int maxBatchSize = 12;
    public float minSpawnInterval = 0.4f; // 防止卡顿
    public float maxSpawnInterval = 5f;

    // ====== 提供便捷方法 ======
    public int GetBatchSize(int wave)
    {
        float size = spawnBatchSize.Evaluate(wave);
        return Mathf.RoundToInt(Mathf.Clamp(size, minBatchSize, maxBatchSize));
    }

    public float GetSpawnInterval(int wave)
    {
        float interval = spawnInterval.Evaluate(wave);
        return Mathf.Clamp(interval, minSpawnInterval, maxSpawnInterval);
    }
}