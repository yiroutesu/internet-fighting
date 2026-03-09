// BackpackUIToolkitDriver.cs (UGUI 版)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 用 UGUI 展示主背包并支持拖拽到垃圾桶销毁，以及与其他实现了 IBackpackUIDriver 的容器互相转移物品。
/// - contentRoot：GridLayout/VerticalLayout 容器
/// - slotPrefab：包含 icon(Image) 和 amount(Text) 的预制体
/// - dragPreview：跟随鼠标的临时图标（Image，RaycastTarget 关闭）
/// - trashDropZone：垃圾桶区域，需要挂 TrashDropZone 组件
/// </summary>
public class BackpackUGUIDriver : MonoBehaviour, IBackpackUIDriver
{
    [SerializeField] private BackpackSO backpack;
    [Header("Layout")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BackpackSlotUGUI slotPrefab;
    [Header("Drag & Drop")]
    [SerializeField] private Image dragPreview;
    [SerializeField] private TrashDropZone trashDropZone;

    private readonly List<BackpackSlotUGUI> slots = new();
    private BackpackSlotUGUI draggingSlot;

    public BackpackSO Data => backpack;

    /// <summary>
    /// 主背包接受所有物品类型。
    /// </summary>
    public bool CanAccept(ItemInstance item) => item != null;

    private void Start()
    {
        if (trashDropZone != null) trashDropZone.Init(this);
        Refresh();
        if (backpack != null) backpack.OnChanged += Refresh;
        if (dragPreview != null) dragPreview.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backpack != null) backpack.OnChanged -= Refresh;
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
        if (backpack == null) return;

        var items = backpack.GetItems();
        EnsureSlotCount(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            slots[i].Bind(items[i], i);
        }
    }

    // 被 slot 调用：开始拖拽
    public void BeginDrag(BackpackSlotUGUI slot, PointerEventData eventData)
    {
        draggingSlot = slot;
        if (dragPreview != null && slot.IconSprite != null)
        {
            dragPreview.transform.Find("Icon").GetComponent<Image>().sprite = slot.IconSprite;
            dragPreview.color  = Color.white;
            dragPreview.rectTransform.position = eventData.position;
            dragPreview.transform.Find("AmountText").gameObject.SetActive(false);
            dragPreview.gameObject.SetActive(true);
        }
        if (trashDropZone != null) trashDropZone.SetHighlight(true);
    }

    // 被 slot 调用：拖拽中
    public void Drag(PointerEventData eventData)
    {
        if (dragPreview != null && dragPreview.gameObject.activeSelf)
            dragPreview.rectTransform.position = eventData.position;
    }

    // 被 slot 调用：结束拖拽（未丢进垃圾桶）
    public void EndDrag()
    {
        draggingSlot = null;
        if (dragPreview != null) dragPreview.gameObject.SetActive(false);
        if (trashDropZone != null) trashDropZone.SetHighlight(false);
    }

    // 垃圾桶回调
    public void DestroyItemFromSlot(BackpackSlotUGUI slot)
    {
        if (backpack == null || slot == null) return;
        backpack.Remove(slot.Index);
        EndDrag();
    }
}
