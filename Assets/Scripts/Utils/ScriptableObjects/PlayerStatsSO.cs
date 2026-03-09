using UnityEngine;
using SIGame.Enums;

[CreateAssetMenu(menuName = "Utils/PlayerStats", fileName = "PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("生命值")]
    [Tooltip("最大生命值")]
    public float maxHP = 100f;
    
    [Header("攻击属性")]
    [Tooltip("攻击伤害")]
    public float attackDamage = 100f;
    
    [Tooltip("攻击速度（次/秒）")]
    public float attackSpeed = 1f;
    
    [Tooltip("攻击范围")]
    public float attackRange = 0f;
    
    [Header("移动属性")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 30f;
    
    [Header("防御属性")]
    [Tooltip("护甲值")]
    public float armor = 0f;
    
    [Header("暴击属性")]
    [Tooltip("暴击率（百分比，如 20 表示 20%）")]
    public float critChance = 0f;
    
    [Tooltip("暴击倍率（如 1.5 表示 150% 伤害）")]
    public float critMultiplier = 1.5f;
    
    [Header("其他属性")]
    [Tooltip("击退力")]
    public float knockBackForce = 20f;
    [Tooltip("射速（同 AttackSpeed，可合并）")]
    public float fireRate = 1f;
    
    [Tooltip("经验拾取范围")]
    public float xpPickRange = 3f;
    
    [Tooltip("幸运值（用于特殊掉落）")]
    public float luck = 0f;
    
    [Header("生命恢复")]
    [Tooltip("每秒自然回血")]
    public float regenPerSec = 0f;
    
    [Tooltip("受伤后多久开始回血（秒）")]
    public float regenDelay = 3f;
    
    [Header("受击无敌")]
    [Tooltip("受击后无敌持续时间（秒）")]
    public float invincibleDuration = 0.5f;
    
    /// <summary>
    /// 根据 PlayerStatAttr 枚举获取对应的基础值
    /// </summary>
    public float GetBaseValue(PlayerStatAttr stat)
    {
        return stat switch
        {
            PlayerStatAttr.MaxHP => maxHP,
            PlayerStatAttr.AttackDamage => attackDamage,
            PlayerStatAttr.AttackSpeed => attackSpeed,
            PlayerStatAttr.MoveSpeed => moveSpeed,
            PlayerStatAttr.Armor => armor,
            PlayerStatAttr.CritChance => critChance,
            PlayerStatAttr.CritMultiplier => critMultiplier,
            PlayerStatAttr.KnockBackForce=> knockBackForce,
            PlayerStatAttr.FireRate => fireRate,
            PlayerStatAttr.AttackRange => attackRange,
            PlayerStatAttr.XPpickRange => xpPickRange,
            PlayerStatAttr.Luck => luck,
            PlayerStatAttr.CurrentHP => 0f, // CurrentHP 通常不设置基础值
            _ => 0f
        };
    }
}

