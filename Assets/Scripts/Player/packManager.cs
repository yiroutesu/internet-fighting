using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class packManager : MonoBehaviour
{
    public BackpackSO mainBackpack;      // 主背包
    public BackpackSO propsBag;      // 装备背包
    public BackpackSO weaponsBag;     // 武器背包
    public BackpackSO missionBag;        // 任务道具背包

    private Dictionary<string, BackpackSO> bags;

    void Awake()
    {
        bags = new()
        {
            ["main"]      = mainBackpack,
            ["equipment"] = propsBag,
            ["quick"]     = weaponsBag,
            ["mission"]   = missionBag
        };
    }

    public BackpackSO GetBag(string id)
    {
    return bags.TryGetValue(id, out var b) ? b : null;
    } 
        
}
