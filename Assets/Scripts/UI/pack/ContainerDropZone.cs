using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 通用容器接收区：
/// - 掛在某个 Panel / 区域上
/// - 在 OnDrop 时，从来源容器移除物品并尝试加入目标容器
/// - 利用 IBackpackUIDriver 做类型检查（Prop / Weapon 等）
/// 使用方法：
/// - 在 Inspector 中把 driverBehaviour 绑定到实现了 IBackpackUIDriver 的脚本（例如 BackpackUGUIDriver / UIPropZone / UIweaponZone）
/// </summary>
public class ContainerDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private MonoBehaviour driverBehaviour; // 必须实现 IBackpackUIDriver

    private IBackpackUIDriver driver;

    private void Awake()
    {
        driver = driverBehaviour as IBackpackUIDriver;
        if (driver == null && driverBehaviour != null)
        {
            Debug.LogError($"{name}: driverBehaviour 没有实现 IBackpackUIDriver 接口");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (driver == null) return;

        var fromSlot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<BackpackSlotUGUI>()
            : null;
        if (fromSlot == null) return;

        var fromDriver = fromSlot.Driver;
        if (fromDriver == null || fromDriver.Data == null || driver.Data == null)
        {
            // 即使条件不满足，也要结束拖拽以隐藏预览
            fromDriver?.EndDrag();
            return;
        }

        // 同一容器内的拖拽：这里暂时不处理（可以以后扩展为换位/排序）
        if (ReferenceEquals(fromDriver, driver))
        {
            fromDriver.EndDrag();
            return;
        }

        // 从来源容器移除整格物品
        var sourceData = fromDriver.Data;
        ItemInstance item = sourceData.Remove(fromSlot.Index, int.MaxValue);
        if (item == null)
        {
            fromDriver.EndDrag();
            return;
        }

        // 目标不接受该类型，尝试放回原容器
        if (!driver.CanAccept(item) || !driver.Data.Add(item))
        {
            if (!sourceData.Add(item))
            {
                Debug.LogWarning("ContainerDropZone: 无法将物品放入任何容器，物品被丢弃。");
            }
        }

        // 无论成功还是失败，都要结束拖拽以隐藏预览
        fromDriver.EndDrag();
    }
}

