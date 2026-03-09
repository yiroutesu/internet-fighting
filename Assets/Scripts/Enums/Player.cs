
namespace SIGame.Enums
{
    //player属性
    public enum PlayerStatAttr
    {
        MaxHP,          // 最大生命值
        CurrentHP,      // 当前生命值（通常不直接加成）
        AttackDamage,   // 攻击伤害
        AttackSpeed,    // 攻击速度（次/秒）
        MoveSpeed,      // 移动速度（单位/秒）
        Armor,          // 护甲（减伤）
        KnockBackForce, //击退力
        CritChance,     // 暴击率（百分比，如 20 = 20%）
        CritMultiplier, // 暴击倍率（如 1.5 = 150% 伤害）
        FireRate,       // 射速（同 AttackSpeed，可合并）
        AttackRange,          // 攻击范围
        XPpickRange,//经验拾取范围
        Luck            // 幸运值（用于特殊掉落）
    }

    public enum PlayerModAttr
    {
        Flat,      // 绝对值如+20 攻击
        Percent,   // 百分比如+15 % 攻击
    }
}