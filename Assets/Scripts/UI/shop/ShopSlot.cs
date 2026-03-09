using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text aboutText;
    [SerializeField] private TMP_Text XP;
    [SerializeField] private TMP_Text Name;
    private ItemInstance myItem;
    private ShopUI shopUI;

    /// <summary>
    /// 外部只要调一次 Setup，就能完成所有显示+点击事件绑定
    /// </summary>
    public void Setup(ItemInstance item, ShopUI ui, Transform parent)
    {
        myItem = item;
        shopUI = ui;

        // 如果自己是刚 Instantiate 出来的，需要挂到父节点
        transform.SetParent(parent, false);

        // 显示
        iconImage.sprite = item.Asset.Icon;
        aboutText.text   = item.Asset.about;
        XP.text="XP:"+item.Asset.XP.ToString();
        Name.text=item.Asset.ItemName;

        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    private void OnClicked() => shopUI.Purchase(this, myItem);
}