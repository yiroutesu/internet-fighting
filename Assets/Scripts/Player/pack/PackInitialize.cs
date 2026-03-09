using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct GearEntry
{
    public ItemAssetSO asset;
    public int amount;
}
public class PackInitialize : MonoBehaviour
{
    
    [SerializeField]private List<GearEntry> firstitems;
    [SerializeField] private BackpackSO backpack;
    private void Start()
    {
        if (firstitems!=null)
        {
            foreach(var item in firstitems)
            {
                backpack.Add(item.asset.CreateInstance(item.amount));
                Debug.Log("装配");
            }
        }
        EquippedItemsManager.Instance.PropAttrCalculate();
    }
}
