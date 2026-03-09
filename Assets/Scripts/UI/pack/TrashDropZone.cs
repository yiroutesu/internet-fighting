using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 垃圾桶区域：接收拖拽丢弃的格子。
/// 通过 IBackpackUIDriver 删除物品，支持多个不同的容器驱动（主背包 / 道具区 / 武器区等）。
/// </summary>
public class TrashDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image highlight;

    private IBackpackUIDriver driver;
    private Color originColor;

    public void Init(IBackpackUIDriver owner)
    {
        driver = owner;
        if (highlight != null) originColor = highlight.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var slot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<BackpackSlotUGUI>()
            : null;
        if (slot != null) driver?.DestroyItemFromSlot(slot);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHighlight(true);
    public void OnPointerExit(PointerEventData eventData)  => SetHighlight(false);

    public void SetHighlight(bool on)
    {
        if (highlight == null) return;
        highlight.color = on ? Color.red : originColor;
    }
}