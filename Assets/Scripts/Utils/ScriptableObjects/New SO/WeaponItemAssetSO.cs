// Assets/Scripts/Items/WeaponItemAssetSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Item")]
public class WeaponItemAssetSO : ItemAssetSO // ← 移除 , IEquipable
{
    
    public GameObject weaponPrefab;

    public override ItemInstance CreateInstance(int stack = 1)
    {
        return new WeaponItemInstance(this);
    }

    // ❌ 删除 OnEquip 和 OnUnEquip 方法！
}