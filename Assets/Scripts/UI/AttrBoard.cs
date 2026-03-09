using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AttrBoard : MonoBehaviour
{
    [SerializeField] private List<AttrItem> attributes; // 在 Inspector 填数据

    [SerializeField] private VisualElement rowTemplate; // 拖拽 AttrRow.uxml 到这里（可选，也可代码加载）

    private VisualElement rootUI;

    private VisualElement attrRoot;

    private string AttrPanel = "AttrPanel";

    void OnEnable()
    {
        rootUI = GetComponent<UIDocument>().rootVisualElement;

        attrRoot = rootUI.Q<VisualElement>(AttrPanel);
        attrRoot.Add(rowTemplate);
        foreach (var attr in attributes)
        {
            var row = CreateAttrRow(attr);
            rootUI.Add(row);
        }
    }

     private VisualElement CreateAttrRow(AttrItem attr)
    {
        var template = Resources.Load<VisualTreeAsset>("Assets/art/UI/PlayerAttributeIcon/fight-tmp.png");
        var row = template.Instantiate();

        // 填充数据
        row.Q<Label>("Value").text = $"{attr.value}{attr.unit}";
        row.Q<Label>("CnName").text = attr.cnName;

        // 设置图标（可选）
        var iconElem = row.Q<VisualElement>("Icon");
        if (!string.IsNullOrEmpty(attr.icon))
        {
            iconElem.style.backgroundImage = Resources.Load<Texture2D>(attr.icon);
        }

        return row;
    }
}
