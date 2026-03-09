using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店面板：初始化 & 刷新时从 WeightedItemDrawer 抽 3~5 个道具，
/// 展示 Icon + About，玩家点击后把道具加入 BackpackSO 并下架。
/// </summary>
public class ShopUI : MonoBehaviour
{
    private WeightedItemDrawer drawer;
    public BackpackSO playerBackpack;
    private Transform slotParent;          // 仅当父节点用
    public ShopSlot slotPrefab;           // 直接拖“ShopSlot 预设”
    [Range(3, 5)] public int drawCount = 4;
    public KeyCode refreshKey = KeyCode.R;

    private readonly List<ShopSlot> slots = new();

    private void Awake()
    {
        drawer = GetComponent<WeightedItemDrawer>();
        slotParent = gameObject.GetComponent<Transform>();
    }
    private void Start()
    {
        Refresh();
    }
    public void Refresh()
    {
        // 1. 清旧
        foreach (var s in slots) Destroy(s.gameObject);
        slots.Clear();

        // 2. 抽新
        var items = drawer.Draw(drawCount, allowDup: false);

        // 3. 建 UI —— 一句话 Instantiate，预设自己会 Setup
        foreach (var ins in items)
        {
            // 预设里已经挂好 ShopSlot 脚本，实例化后直接调用 Setup
            ShopSlot slot = Instantiate(slotPrefab, slotParent);
            slot.Setup(ins, this, slotParent);
            slots.Add(slot);
        }
    }

    public void Purchase(ShopSlot slot, ItemInstance ins)
    {
        if (EconomyManager.Instance.TryRemoveGold(ins.Asset.XP))
        {
            if (!playerBackpack.Add(ins))
            {
                EconomyManager.Instance.TryRemoveGold(-ins.Asset.XP);
                Debug.Log("背包已满!");
                return;
            }
            slots.Remove(slot);
            Destroy(slot.gameObject);
            AudioManager.instance.Play("buying");
        }
    }
}