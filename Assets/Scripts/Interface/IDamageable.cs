// Assets/Scripts/Enemies/IDamageable.cs
using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// 目标是否已经死亡（不可交互、不可攻击）
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// 受到伤害时调用
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="source">伤害来源（如玩家、子弹等）</param>
    void TakeDamage(DamageInfo info);
    /// <summary>
    /// 获取固定防御值（如护甲）
    /// </summary>
    float GetDefense();

    /// <summary>
    /// 获取百分比免伤（0.0～1.0，如 0.2 = 20% 免伤）
    /// </summary>
    float GetDamageReduction();
}