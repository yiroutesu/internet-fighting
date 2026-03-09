// Assets/Scripts/Items/PassiveItemInstance.cs
using UnityEngine;

public class PassiveItemInstance : ItemInstance, IStackable, IEquipable
{
    public int Stack { get; set; }
    private int appliedCount = 0;
    private bool isEquipped = false; // ← 新增：避免重复装备/卸载

    public PassiveItemInstance(PassiveItemAssetSO asset, int stack) : base(asset)
    {
        Stack = Mathf.Max(1, stack);
    }

    public void OnEquip(PlayerController player)
    {
        if (player == null || isEquipped) return;

        var passive = (PassiveItemAssetSO)Asset;
        int delta = Stack - appliedCount;

        if (delta > 0)
        {
            foreach (var mech in passive.mechanics)
                mech.Apply(player, delta);
        }
        else if (delta < 0)
        {
            // 理论上不会发生（因为 OnEquip 只在新增时调用），但安全起见
            foreach (var mech in passive.mechanics)
                mech.Remove(player, -delta);
        }

        appliedCount = Stack;
        isEquipped = true;
    }

    public void OnUnEquip(PlayerController player)
    {
        if (player == null || !isEquipped || appliedCount <= 0) return;

        var passive = (PassiveItemAssetSO)Asset;
        foreach (var mech in passive.mechanics)
            mech.Remove(player, appliedCount);

        appliedCount = 0;
        isEquipped = false;
    }

    // 🔥 关键：当 Stack 被外部修改（如合并堆叠）时，需通知重新应用
    // 但 BackpackSO 目前没有提供“堆叠变化”事件，所以通常靠 ReequipAll 解决
}