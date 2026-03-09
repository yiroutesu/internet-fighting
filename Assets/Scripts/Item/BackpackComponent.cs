using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在玩家身上的运行时背包组件，持有 BackpackSO 实例
/// </summary>
public class BackpackComponent : MonoBehaviour
{
    [Tooltip("拖入你的 BackpackSO 实例（如 PlayerBackpack.asset）")]
    public BackpackSO backpackData;

    public bool Add(ItemInstance item)
    {
        return backpackData?.Add(item) == true;
    }

    // 可选：提供只读访问
}
