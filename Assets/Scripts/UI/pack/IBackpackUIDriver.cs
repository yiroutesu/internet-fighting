using UnityEngine.EventSystems;

/// <summary>
/// 通用背包 UI 驱动接口：
/// - 提供背包数据引用（Data）
/// - 提供类型过滤（CanAccept）
/// - 提供拖拽相关回调，供格子与垃圾桶、容器间交互调用
/// </summary>
public interface IBackpackUIDriver
{
    /// <summary>当前 UI 绑定的背包数据。</summary>
    BackpackSO Data { get; }

    /// <summary>该容器是否接受这个物品（例如 Prop / Weapon 过滤）。</summary>
    bool CanAccept(ItemInstance item);

    /// <summary>开始从某个格子拖拽。</summary>
    void BeginDrag(BackpackSlotUGUI slot, PointerEventData eventData);

    /// <summary>拖拽中。</summary>
    void Drag(PointerEventData eventData);

    /// <summary>结束拖拽（不一定成功放入任何容器）。</summary>
    void EndDrag();

    /// <summary>从某个格子彻底删除物品（用于垃圾桶）。</summary>
    void DestroyItemFromSlot(BackpackSlotUGUI slot);
}


