#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BossStateMachineController))]
public class BossStateMachineControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        BossStateMachineController controller = (BossStateMachineController)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("初始化默认规则"))
        {
            controller.InitializeDefaultRules();
            EditorUtility.SetDirty(controller);
        }
        
        if (GUILayout.Button("清除所有规则"))
        {
            controller.transitionRules.Clear();
            EditorUtility.SetDirty(controller);
        }
        
        if (Application.isPlaying && controller.fsm != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("调试信息", MessageType.Info);
            EditorGUILayout.LabelField("当前状态", controller.currentState?.Name ?? "None");
            EditorGUILayout.LabelField("上一个状态", controller.lastState?.Name ?? "None");
            
            if (GUILayout.Button("手动评估下一个状态"))
            {
                var nextState = controller.EvaluateNextState();
                Debug.Log($"建议的下一个状态: {nextState?.Name ?? "None"}");
                Debug.Log(controller.GetWeightsDebugInfo());
            }
        }
    }
}
#endif