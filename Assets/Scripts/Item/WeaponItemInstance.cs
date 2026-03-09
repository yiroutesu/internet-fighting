// Assets/Scripts/Items/WeaponItemInstance.cs
using UnityEngine;
//这个是实例化武器的
public class WeaponItemInstance : ItemInstance, IEquipable
{
    private GameObject equippedWeaponObject; // ← 保存生成的武器实例

    public WeaponItemInstance(WeaponItemAssetSO asset) : base(asset) { }

    public void OnEquip(PlayerController player)
    {
        if (player == null || equippedWeaponObject != null) return;

        var weaponAsset = (WeaponItemAssetSO)Asset;
        if (weaponAsset.weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is missing!");
            return;
        }

        // 实例化武器（作为玩家子物体或世界对象）
        equippedWeaponObject = Object.Instantiate(
            weaponAsset.weaponPrefab,
            player.transform.position,
            Quaternion.identity
        );

        // 关键：初始化武器行为
        if (equippedWeaponObject.TryGetComponent(out WeaponBehavior behavior))
        {
            behavior.Initialize(player.gameObject);
        }
        else
        {
            Debug.LogWarning($"Weapon prefab missing WeaponBehavior: {weaponAsset.name}");
            Object.Destroy(equippedWeaponObject);
            equippedWeaponObject = null;
        }
    }

    public void OnUnEquip(PlayerController player)
    {
        if (equippedWeaponObject != null)
        {
            Object.Destroy(equippedWeaponObject);
            equippedWeaponObject = null;
        }
    }
}