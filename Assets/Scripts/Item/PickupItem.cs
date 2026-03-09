// Assets/Scripts/Items/PickupItem.cs
using UnityEngine;

/// <summary>
/// 场景中的可拾取道具对象
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Tooltip("拖入一个 ItemAssetSO（如 PassiveItemAssetSO）")]
    public ItemAssetSO itemAsset;

    [Tooltip("拾取数量（对可堆叠道具有效）")]
    public int pickupAmount = 1; // ← 新增字段

    [Tooltip("拾取后是否自动添加到玩家背包？")]
    public bool autoPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        if (autoPickup && other.CompareTag("Player"))
        {
            // ✅ 正确方式：通过 BackpackComponent 访问背包
            var backpackComp = other.GetComponent<BackpackComponent>();
            if (backpackComp == null)
            {
                Debug.LogError("<空");
            }
            if (backpackComp != null)
            {
                TryAddToBackpack(backpackComp);
                Debug.Log("已加入背包");
            }
        }

        
        Debug.Log("已接触");
    }

    private void TryAddToBackpack(BackpackComponent backpackComp)
    {
        if (itemAsset == null || backpackComp == null) return;

        // 支持堆叠数量
        var itemInstance = itemAsset.CreateInstance(pickupAmount);
        if (itemInstance != null && backpackComp.Add(itemInstance))
        {
            // 拾取成功：播放音效、特效等（此处省略）
            Destroy(gameObject);
            Debug.Log($"Picked up {itemAsset.id} x{pickupAmount}");
        }
        else
        {
            Debug.LogWarning($"Failed to pick up {itemAsset.id}");
        }
    }
}