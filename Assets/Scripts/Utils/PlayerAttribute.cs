using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// AttrItem.cs
[System.Serializable]
public struct AttrItem
{
    public string id;      // 属性标识，如 "speed"
    public string icon;    // 图标资源路径，如 "Icons/speed"
    public float  value;   // 当前数值
    public string unit;    // 单位，如 "%" 或 "点"
    public string cnName;  // 中文名，如 "移动速度"
}