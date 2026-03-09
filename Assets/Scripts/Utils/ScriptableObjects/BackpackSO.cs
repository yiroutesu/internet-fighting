using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Backpack")]
public class BackpackSO : ScriptableObject, IEnumerable<ItemInstance>
{
    [SerializeField] private List<ItemInstance> items = new();
    public event System.Action OnChanged;
    public int Capacity = 36;

    public List<ItemInstance> GetItems()
    {
        return items;
    }
    public bool Add(ItemInstance ins)
    {
        bool ok = false;
        if (ins is IStackable s)
        {
            var same = items.Find(x => x.Asset.ID == ins.Asset.ID) as IStackable;
            if (same != null) { same.Stack += s.Stack; ok = true; }
        }
        if (!ok && items.Count < Capacity) { items.Add(ins); ok = true; }

        if (ok) OnChanged?.Invoke();        // ← 触发
        return ok;
    }


    public ItemInstance Remove(int index, int amount = 1)
{
    if (index < 0 || index >= items.Count) return null;

    ItemInstance it = items[index];

    // 情况1：可堆叠且剩余足够
    if (it is IStackable s && s.Stack > amount)
    {
        s.Stack -= amount;                     // 原堆数量减少
        ItemInstance clone = it.Asset.CreateInstance(amount);
        OnChanged?.Invoke();                   // 数据已变
        return clone;                          // 返回新实例
    }

    // 情况2：整格删除
    items.RemoveAt(index);
    OnChanged?.Invoke();                       // 数据已变
    return it;                                 // 返回原实例
}

    public void Clear()
    {
        if (items.Count == 0) return; // 提前退出，避免无谓调用事件

        items.Clear();
        OnChanged?.Invoke(); // 通知所有监听者（如 EquippedItemsManager、UI 等）
    }
    // 迭代器方便 UI 绑定
    public IEnumerator<ItemInstance> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
