// Assets/Scripts/Player/EquippedItemsManager.cs
using System.Collections.Generic;
using UnityEngine;
using SIGame.Enums;
using UnityEngine.Events;
public class EquippedItemsManager : MonoBehaviour
{
    public static EquippedItemsManager Instance;
    public BackpackSO weaponPack;
    public BackpackSO propPack;
    private List<IEquipable> equipped = new();
    private PlayerController player;

    void Awake()
    {
         if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        player = GetComponent<PlayerController>();

        if (weaponPack != null)
        {
            weaponPack.OnChanged += ReequipAll;
        }
        if (propPack != null)
            propPack.OnChanged += RefreshPropAttributes; // ← 新增
    }

    void Start()
    {
        ReequipAll();
    }

    void OnDestroy()
    {
        if (weaponPack != null)
        {
            weaponPack.OnChanged -= ReequipAll;
        }
        // 卸载所有
        foreach (var eq in equipped)
            eq.OnUnEquip(player);
    }

    public void ReequipAll()
    {
        // 卸载旧的
        foreach (var eq in equipped)
            eq.OnUnEquip(player);
        equipped.Clear();

        // 重新装备新的
        if (weaponPack != null)
        {
            foreach (var item in weaponPack)
            {
                if (item is IEquipable eq)
                {
                    eq.OnEquip(player);
                    equipped.Add(eq);
                }
            }
        }
    }
    
    /// <summary>
    /// 计算并应用背包中所有道具的属性加成到玩家身上
    /// </summary>
    public void PropAttrCalculate()
    {
        Debug.Log("属性被计算");
        if (propPack == null)
        {
            Debug.LogWarning("PropAttrToPlayer: proppack 未设置！", this);
            return;
        }

        // 获取玩家控制器和属性系统
        if (player == null)
        {
            player = GetComponent<PlayerController>();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        if (player == null || player.statSystem == null)
        {
            Debug.LogError("PropAttrToPlayer: 找不到 PlayerController 或 StatSystem！", this);
            return;
        }

        StatSystem statSystem = player.statSystem;
        
        // 获取背包中的所有道具
        List<ItemInstance> items = propPack.GetItems();
        
        // 遍历所有道具
        foreach (ItemInstance item in items)
        {
            // 检查是否是道具实例
            if (item is PropInstance propInstance)
            {
                // 获取道具资产
                if (propInstance.Asset is PropAsset propAsset)
                {
                    // 获取堆叠数量
                    int stackCount = propInstance.Stack;
                    
                    // 遍历道具的所有属性修改
                    foreach (StatMod statMod in propAsset.StatMods)
                    {
                        // 计算总加成值（考虑堆叠）
                        float totalValue = statMod.value * stackCount;
                        
                        // 根据修改类型应用加成
                        if (statMod.type == PlayerModAttr.Flat)
                        {
                            statSystem.AddFlatModifier(statMod.stat, totalValue);
                        }
                        else if (statMod.type == PlayerModAttr.Percent)
                        {
                            statSystem.AddPercentModifier(statMod.stat, totalValue);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 移除背包中所有道具施加在玩家身上的属性加成
    /// </summary>
    public void PropAttrRemove()
    {
        if (propPack == null)
        {
            Debug.LogWarning("PropAttrToPlayer: proppack 未设置！", this);
            return;
        }

        // 获取玩家控制器和属性系统
        if (player == null)
        {
            player = GetComponent<PlayerController>();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        if (player == null || player.statSystem == null)
        {
            Debug.LogError("PropAttrToPlayer: 找不到 PlayerController 或 StatSystem！", this);
            return;
        }

        StatSystem statSystem = player.statSystem;
        
        // 获取背包中的所有道具
        List<ItemInstance> items = propPack.GetItems();
        
        // 遍历所有道具
        foreach (ItemInstance item in items)
        {
            // 检查是否是道具实例
            if (item is PropInstance propInstance)
            {
                // 获取道具资产
                if (propInstance.Asset is PropAsset propAsset)
                {
                    // 获取堆叠数量
                    int stackCount = propInstance.Stack;
                    
                    // 遍历道具的所有属性修改
                    foreach (StatMod statMod in propAsset.StatMods)
                    {
                        // 计算总加成值（考虑堆叠）
                        float totalValue = statMod.value * stackCount;
                        
                        // 根据修改类型移除加成
                        if (statMod.type == PlayerModAttr.Flat)
                        {
                            statSystem.RemoveFlatModifier(statMod.stat, totalValue);
                        }
                        else if (statMod.type == PlayerModAttr.Percent)
                        {
                            statSystem.RemovePercentModifier(statMod.stat, totalValue);
                        }
                    }
                }
            }
        }
    }
    // 新方法：刷新道具属性
    private void RefreshPropAttributes()
    {
        Debug.Log("属性Ref");
        PropAttrRemove();     // 先移除旧的
        PropAttrCalculate();  // 再应用新的（此时背包已空，所以无加成）
    }
    // 清空武器背包（会自动重新装备 → 实际卸下所有）
    public void ClearWeaponPack()
    {
        weaponPack?.Clear();
    }

    // 清空道具背包（需手动更新属性，除非你已订阅 OnChanged）
    public void ClearPropPack()
    {
        propPack?.Clear();
    }
    public void SetXPpickRange()
    {
        var XPCollider=GetComponentInChildren<SphereCollider>();
        XPCollider.radius=player.GetComponent<StatSystem>().GetFinalValue(PlayerStatAttr.XPpickRange);
    }
}