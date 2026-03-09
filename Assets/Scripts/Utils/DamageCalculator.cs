// Assets/Scripts/Utilities/DamageCalculator.cs
using UnityEngine;
using SIGame.Enums;
using SIGame.Stats;

public static class DamageCalculator
{
    // 平衡常数，用于护甲公式（可后续配置化）
    public const float ARMOR_CONSTANT = 100f;

    /// <summary>
    /// 根据原始伤害和目标防御属性，计算最终实际伤害
    /// </summary>
    public static float CalculateFinalDamage(float rawDamage, IDamageable target)
    {
        if (rawDamage <= 0 || target == null) 
            return 0f;

        // 1. 护甲减免（固定防御 → 百分比减免）
        float defense = target.GetDefense();
        float armorReduction = defense / (defense + ARMOR_CONSTANT);
        armorReduction = Mathf.Clamp01(armorReduction);

        // 2. 额外百分比免伤（如技能、Buff）
        float extraReduction = Mathf.Clamp01(target.GetDamageReduction());

        // 3. 合并减伤（通常为叠加，非乘算；也可改为乘算：1 - (1-a)*(1-b)）
        float totalReduction = Mathf.Clamp01(armorReduction + extraReduction);

        // 4. 最终伤害（可选：保留最小伤害，避免完全免疫）
        float finalDamage = rawDamage * (1f - totalReduction);

        // 可选：最低造成 5% 原始伤害（防“无限护甲”卡死）
        // finalDamage = Mathf.Max(finalDamage, rawDamage * 0.05f);

        return finalDamage;
    }

    // 其他方法保持不变...
    public static (float damage, bool isCritical) CalculateMeleeDamageWithCritFlag(
        IStatSystem statSystem, 
        float attackMagnification = 1f)
    {
        if (statSystem == null) return (0f, false);

        float baseAttack = statSystem.GetFinalValue(PlayerStatAttr.AttackDamage);
        float critChance = statSystem.GetFinalValue(PlayerStatAttr.CritChance);
        float critMultiplier = statSystem.GetFinalValue(PlayerStatAttr.CritMultiplier);

        bool isCritical = Random.value * 100 < critChance;
        float damage = baseAttack * attackMagnification;

        if (isCritical)
        {
            damage *= critMultiplier;
        }

        return (damage, isCritical);
    }

    public static float GetKnockbackForce(IStatSystem statSystem)
    {
        return statSystem?.GetFinalValue(PlayerStatAttr.KnockBackForce) ?? 0f;
    }
}