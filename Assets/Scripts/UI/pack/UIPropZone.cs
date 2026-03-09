using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 道具容器 UI：
/// - 绑定一个 BackpackSO（通常是 packManager.propsBag）
/// - 使用 BackpackSlotUGUI 展示格子
/// - 实现 IBackpackUIDriver，支持与其他容器（主背包 / 武器区）互相拖拽转移
/// - 只接受 Prop 类型物品
/// </summary>
public class UIPropZone : MonoBehaviour, IBackpackUIDriver
{
    [SerializeField] private BackpackSO propBag;

    [Header("Layout")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BackpackSlotUGUI slotPrefab;

    [Header("Drag & Drop")]
    [SerializeField] private Image dragPreview;

    private readonly List<BackpackSlotUGUI> slots = new();
    private BackpackSlotUGUI draggingSlot;

    public BackpackSO Data => propBag;
    [Header("Accept Filter")]
    [Tooltip("留空=接受所有；填一个或多个派生类=只接受这些类型")]
    [SerializeField] private List<ItemAssetSO> acceptTypes;   // 拖 WeaponItemAssetSO、PropAsset 都行

    private void Start()
    {
        if (propBag != null) propBag.OnChanged += Refresh;
        Refresh();
        if (dragPreview != null) dragPreview.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (propBag != null) propBag.OnChanged -= Refresh;
    }

    private void EnsureSlotCount(int count)
    {
        while (slots.Count < count)
        {
            var slot = Instantiate(slotPrefab, contentRoot);
            slot.Init(this);
            slots.Add(slot);
        }
        while (slots.Count > count)
        {
            var last = slots[slots.Count - 1];
            slots.RemoveAt(slots.Count - 1);
            Destroy(last.gameObject);
        }
    }

    private void Refresh()
    {
        if (propBag == null) return;

        var items = propBag.GetItems();
        EnsureSlotCount(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            slots[i].Bind(items[i], i);
        }
    }

    /// <summary>只接受 PropAsset 生成的道具。</summary>
    public bool CanAccept(ItemInstance item)
    {
        if (item == null || item.Asset == null) return false;

        // 白名单为空 → 兼容旧逻辑：全接受
        if (acceptTypes == null || acceptTypes.Count == 0) return true;

        // 只要 asset 是白名单里任意一个派生类就通过
        bool ok = acceptTypes.Exists(t => item.Asset.GetType() == t.GetType());
        return ok;
    }

    // 被 slot 调用：开始拖拽
    public void BeginDrag(BackpackSlotUGUI slot, PointerEventData eventData)
    {
        draggingSlot = slot;
        if (dragPreview != null && slot.IconSprite != null)
        {
            dragPreview.transform.Find("Icon").GetComponent<Image>().sprite = slot.IconSprite;
            dragPreview.color = Color.white;
            dragPreview.rectTransform.position = eventData.position;
            dragPreview.transform.Find("AmountText").gameObject.SetActive(false);
            dragPreview.gameObject.SetActive(true);

        }
    }

    // 被 slot 调用：拖拽中
    public void Drag(PointerEventData eventData)
    {
        if (dragPreview != null && dragPreview.gameObject.activeSelf)
            dragPreview.rectTransform.position = eventData.position;
    }

    // 被 slot 调用：结束拖拽（未丢进其他容器 / 垃圾桶）
    public void EndDrag()
    {
        draggingSlot = null;
        if (dragPreview != null) dragPreview.gameObject.SetActive(false);
    }

    // 垃圾桶回调（如果有为此容器配置 TrashDropZone）
    public void DestroyItemFromSlot(BackpackSlotUGUI slot)
    {
        if (propBag == null || slot == null) return;
        propBag.Remove(slot.Index, int.MaxValue);
        EndDrag();
    }
}
