using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
/// <summary>
/// 单个格子：展示图标/数量，并把拖拽事件传回驱动。
/// </summary>
public class BackpackSlotUGUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amount;

    private IBackpackUIDriver driver;
    private CanvasGroup canvasGroup;

    public int Index { get; private set; }
    public Sprite IconSprite => icon != null ? icon.sprite : null;
    public IBackpackUIDriver Driver => driver;

    public void Init(IBackpackUIDriver owner)
    {
        driver = owner;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(ItemInstance ins, int index)
    {
        Index = index;
        if (icon != null) icon.sprite = ins?.Asset?.Icon;
        if (amount != null) amount.text = ins is IStackable s ? s.Stack.ToString() : "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        driver?.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        driver?.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        driver?.EndDrag();
    }
}