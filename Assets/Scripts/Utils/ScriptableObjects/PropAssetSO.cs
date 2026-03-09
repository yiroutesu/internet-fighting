using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Prop")]
public class PropAsset : ItemAssetSO
{
    public List<StatMod> StatMods = new();

    public override ItemInstance CreateInstance(int stack = 1)
    {
        return new PropInstance(this, stack);
    }
}
