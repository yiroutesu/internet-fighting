using System.Collections;
using System.Collections.Generic;
using SIGame.Enums;
using UnityEngine;
public abstract class ItemAssetSO : ScriptableObject
{
    [Header("通用")]
    public string ID = System.Guid.NewGuid().ToString(); // 全局唯一
    public string id;
    public string ItemName;
    public string about;
    public float weight;
    public Sprite Icon;
    public int MaxStack = 1;
    public int XP=20;

    // 生成运行时实体
    public abstract ItemInstance CreateInstance(int stack = 1);
}
public interface IEquipable
{
    void OnEquip(PlayerController player);
    void OnUnEquip(PlayerController player);
}
public interface IStackable      { int Stack { get; set; } }
public interface ITriggerable    { void Trigger(); }   
public interface ICollector      { void OnPickup(); }
public abstract class ItemInstance
{
    public readonly ItemAssetSO Asset;
    protected ItemInstance(ItemAssetSO asset) => Asset = asset;
}
public class PropInstance : ItemInstance, IStackable
{
    public int Stack { get; set; }   // 真正的“可变”数据
    public PropInstance(PropAsset a, int stack) : base(a) => Stack = stack;
}
[System.Serializable]
public struct StatMod
{
    public PlayerStatAttr  stat;     // 枚举：ATK、DEF、SPD、MaxHP…
    public float     value;    // 绝对值或百分比
    public PlayerModAttr   type;     // Flat / Percent
}
public abstract class Mechanic : ScriptableObject
{
    public abstract void Apply(PlayerController p, int count = 1);
    public abstract void Remove(PlayerController p, int count = 1);
}
