// Assets/Scripts/Mechanics/StatModifierMechanic.cs
using UnityEngine;
using SIGame.Enums;

[CreateAssetMenu(menuName = "Mechanics/Stat Modifier")]
public class StatModifierMechanic : Mechanic
{
    public StatMod mod;

    public override void Apply(PlayerController p, int count = 1)
    {
        float total = mod.value * count;
        if (mod.type == PlayerModAttr.Flat)
            p.statSystem.AddFlatModifier(mod.stat, total);
        else if (mod.type == PlayerModAttr.Percent)
            p.statSystem.AddPercentModifier(mod.stat, total);
    }

    public override void Remove(PlayerController p, int count = 1)
    {
        float total = mod.value * count;
        if (mod.type == PlayerModAttr.Flat)
            p.statSystem.RemoveFlatModifier(mod.stat, total);
        else if (mod.type == PlayerModAttr.Percent)
            p.statSystem.RemovePercentModifier(mod.stat, total);
    }
}