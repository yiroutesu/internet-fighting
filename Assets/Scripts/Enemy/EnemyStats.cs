using UnityEngine;

/// <summary>
/// 敌人的静态配置数据（不会在运行时改变）
/// 用于 ScriptableObject 或直接序列化
/// </summary>
[System.Serializable]
public class EnemyStats
{
    [Header("基础信息")]
    public string enemyName = "Enemy";
    public Color tint = Color.white;

    [Header("战斗属性")]
    [Min(1)] public float maxHealth = 50f;
    [Min(0)] public float moveSpeed = 10f;
    [Min(0)] public float damage = 5f;
    [Min(0.1f)] public float contactDamageCooldown = 0.5f;

    // 👇 新增：防御系统
    [Header("防御属性")]
    [Min(0)] public float defense = 0f;          // 固定减伤（如 -10 点伤害）
    [Range(0, 1)] public float damageReduction = 0f; // 百分比免伤（0.2 = 20%）
    
    [Header("射击行为")]
    public bool canShoot = false;
    public string bulletSubKey = "Basic"; // ← 只填子名称，如 "Laser", "ArcherArrow"
    public float shootInterval = 1f;
    public float shootRange = 15f;
    public Transform shootPoint;
    public float bulletKnockBackForce = 5f;

    [Header("奖励")]
    public int scoreValue = 10;
    public int experienceValue = 5;

    [Header("生成配置")]
    public int spawnWeight = 60;

    public int maxAlive = 15;
    

    [Header("行为标记")]
    public bool isBoss = false;
}