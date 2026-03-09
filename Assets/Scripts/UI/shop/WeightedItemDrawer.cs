using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeightedItemDrawer : MonoBehaviour
{
    [Tooltip("是否不放回抽取（false = 放回）")]
    public bool withoutReplacement = true;

    // 缓存所有 SO
    private List<ItemAssetSO> allItems;
    // 缓存对应权重，避免每次都 Sum
    private float totalWeight;

    private void Awake()
    {
        Reload();   // 也可以公开 Reload() 供以后热更重载
    }

    // 手动重载（比如做了热更）
    public void Reload()
    {
        allItems = Resources.LoadAll<ItemAssetSO>("Item").ToList();
        totalWeight = allItems.Sum(so => so.weight);
        if (Mathf.Approximately(totalWeight, 0f))
            Debug.LogWarning("[WeightedItemSpawner] 所有道具 weight 为 0！");
    }

    /// <summary>
    /// 按权重随机抽取 n 个道具，返回 ItemInstance 列表
    /// </summary>
    /// <param name="count">想抽几个</param>
    /// <param name="allowDup">
    /// true  = 放回抽取（允许重复）<br/>
    /// false = 不放回（默认，跟 withoutReplacement 字段保持一致）
    /// </param>
    public List<ItemInstance> Draw(int count, bool? allowDup = null)
    {
        bool dup = allowDup ?? !withoutReplacement;
        if (count <= 0) return new List<ItemInstance>();

        List<ItemInstance> result = new List<ItemInstance>(count);

        /* 1. 如果允许重复，走老逻辑 */
        if (dup)
        {
            for (int i = 0; i < count; i++)
                result.Add(WeightedPick(allItems, totalWeight).CreateInstance(1));
            return result;
        }

        /* 2. 不放回逻辑 */
        var pool = new List<ItemAssetSO>(allItems);
        float poolWeight = totalWeight;

        while (result.Count < count && pool.Count > 0)
        {
            var hit = WeightedPick(pool, poolWeight);
            result.Add(hit.CreateInstance(1));
            pool.Remove(hit);
            poolWeight -= hit.weight;
        }

        /* 3. 库存不足：补齐部分全部给 ID == "001" 的道具 */
        int lack = count - result.Count;
        if (lack > 0)
        {
            var fallback = allItems.Find(so => so.id == "002");
            if (fallback == null)
            {
                Debug.LogWarning("[WeightedItemSpawner] 库存不足且找不到 ID==\"001\" 的道具！");
            }
            else
            {
                for (int i = 0; i < lack; i++)
                    result.Add(fallback.CreateInstance(1));
            }
        }

        return result;
    }

    /* 通用带权随机一次 */
    private ItemAssetSO WeightedPick(IList<ItemAssetSO> list, float total)
    {
        float r = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        foreach (var so in list)
        {
            acc += so.weight;
            if (r <= acc) return so;
        }
        return list[list.Count - 1];   // 浮点误差兜底
    }
}