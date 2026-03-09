// Assets/Scripts/Items/PassiveItemAssetSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Passive Item")]
public class PassiveItemAssetSO : ItemAssetSO
{
    [Header("被动效果")]
    public Mechanic[] mechanics; // ← 仅作为数据模板

    public override ItemInstance CreateInstance(int stack = 1)
    {
        return new PassiveItemInstance(this, stack);
    }

    // ❌ 删除 OnEquip / OnUnEquip！
}