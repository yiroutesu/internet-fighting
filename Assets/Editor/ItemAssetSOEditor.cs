using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemAssetSO),true)]
public class ItemAssetSOEditor : Editor
{
    private bool _initialized;
    private void OnEnable() {
        var so =target as ItemAssetSO;
        if(so==null) return;
        if (Mathf.Approximately(so.weight, 0f))
        {
            //开启 Undo，方便 Ctrl-Z
            Undo.RecordObject(so, "设置默认权重");

            so.weight = 1f;
            EditorUtility.SetDirty(so);

            _initialized = true;
        }   
    }
    public override void OnInspectorGUI()
    {
        // 正常绘制所有字段
        base.OnInspectorGUI();

        // 如果这次初始化过，给个小小提示
        if (_initialized)
        {
            EditorGUILayout.HelpBox("weight 权重默认值为1", MessageType.Info);
            _initialized = false;   // 只提示一次
        }
    }
}