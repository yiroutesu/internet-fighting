using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways] // 在编辑模式下也更新
public class BossCubeFollower : MonoBehaviour
{
    [Header("目标立方体引用")]
    [SerializeField] private BossRotationCube targetCube;
    
    [Header("顶点跟随物体")]
    [SerializeField] private List<GameObject> vertexObjects = new List<GameObject>(8);
    
    [Header("跟随设置")]
    [SerializeField] private bool followInFixedUpdate = true; // 在FixedUpdate中跟随
    [SerializeField] private bool followInUpdate = false;     // 在Update中跟随
    [SerializeField] private bool useLocalPosition = false;   // 使用局部位置
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // 位置偏移
    
    [Header("默认物体设置")]
    [SerializeField] private bool createDefaultIfEmpty = true; // 如果没有指定物体则创建默认
    [SerializeField] private PrimitiveType defaultPrimitiveType = PrimitiveType.Cube; // 默认物体类型
    [SerializeField] private float defaultObjectSize = 0.1f; // 默认物体大小
    [SerializeField] private Material defaultMaterial; // 默认材质
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showVertexLabels = true;
    [SerializeField] private Color labelColor = Color.white;
    
    // 缓存顶点世界位置
    private Vector3[] cachedWorldVertices = new Vector3[8];
    
    // 顶点名称数组（便于识别）
    private readonly string[] vertexNames = {
        "左下前", "右下前", "右后下", "左后下",
        "左上前", "右上前", "右后上", "左后上"
    };
    
    void Start()
    {
        // 如果没有设置目标立方体，尝试查找
        if (targetCube == null)
        {
            targetCube = FindObjectOfType<BossRotationCube>();
            if (targetCube == null)
            {
                Debug.LogWarning($"未找到BossRotationCube组件，请在Inspector中指定或确保场景中有BossRotationCube对象");
            }
        }
        
        // 初始化顶点物体
        InitializeVertexObjects();
    }
    
    void FixedUpdate()
    {
        if (followInFixedUpdate && targetCube != null)
        {
            UpdateObjectsPositions();
        }
    }
    
    void Update()
    {
        // 在编辑模式下也更新位置
        if (!Application.isPlaying)
        {
            UpdateObjectsPositions();
        }
        else if (followInUpdate && targetCube != null)
        {
            UpdateObjectsPositions();
        }
    }
    
    void OnValidate()
    {
        // 确保列表大小为8
        while (vertexObjects.Count < 8)
        {
            vertexObjects.Add(null);
        }
        
        // 在编辑模式下实时更新
        if (!Application.isPlaying && targetCube != null)
        {
            UpdateObjectsPositions();
        }
    }
    
    /// <summary>
    /// 初始化顶点物体
    /// </summary>
    private void InitializeVertexObjects()
    {
        if (vertexObjects == null || vertexObjects.Count == 0)
        {
            vertexObjects = new List<GameObject>(8);
            for (int i = 0; i < 8; i++) vertexObjects.Add(null);
        }
        
        // 检查是否需要创建默认物体
        bool hasAnyObject = false;
        for (int i = 0; i < vertexObjects.Count; i++)
        {
            if (vertexObjects[i] != null)
            {
                hasAnyObject = true;
                break;
            }
        }
        
        // 如果没有物体且允许创建默认物体
        if (!hasAnyObject && createDefaultIfEmpty && Application.isPlaying)
        {
            CreateDefaultObjects();
        }
    }
    
    /// <summary>
    /// 创建默认物体
    /// </summary>
    private void CreateDefaultObjects()
    {
        for (int i = 0; i < 8; i++)
        {
            if (vertexObjects[i] == null)
            {
                // 创建默认物体
                GameObject obj = GameObject.CreatePrimitive(defaultPrimitiveType);
                obj.name = $"{gameObject.name}_Vertex_{i}_{vertexNames[i]}";
                obj.transform.localScale = Vector3.one * defaultObjectSize;
                
                // 设置父对象
                obj.transform.SetParent(transform);
                
                // 设置材质
                if (defaultMaterial != null)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = defaultMaterial;
                    }
                }
                
                vertexObjects[i] = obj;
            }
        }
    }
    
    /// <summary>
    /// 更新物体位置
    /// </summary>
    private void UpdateObjectsPositions()
    {
        if (targetCube == null)
        {
            Debug.LogWarning($"目标立方体未设置，无法更新顶点物体位置");
            return;
        }
        
        // 获取当前顶点世界坐标
        cachedWorldVertices = targetCube.GetWorldVertices();
        
        // 更新每个物体的位置
        for (int i = 0; i < 8 && i < vertexObjects.Count; i++)
        {
            GameObject obj = vertexObjects[i];
            if (obj != null)
            {
                if (useLocalPosition)
                {
                    // 转换为相对于本物体的局部位置
                    Vector3 localPos = transform.InverseTransformPoint(cachedWorldVertices[i]) + positionOffset;
                    obj.transform.localPosition = localPos;
                }
                else
                {
                    // 直接使用世界位置
                    obj.transform.position = cachedWorldVertices[i] + positionOffset;
                }
            }
        }
    }
    
    /// <summary>
    /// 创建并设置默认物体
    /// </summary>
    public void CreateAndSetDefaultObjects()
    {
        CreateDefaultObjects();
        UpdateObjectsPositions();
    }
    
    /// <summary>
    /// 清空所有顶点物体
    /// </summary>
    public void ClearAllObjects()
    {
        for (int i = 0; i < vertexObjects.Count; i++)
        {
            if (vertexObjects[i] != null && vertexObjects[i].transform.parent == transform)
            {
                if (Application.isPlaying)
                {
                    Destroy(vertexObjects[i]);
                }
                else
                {
                    DestroyImmediate(vertexObjects[i]);
                }
            }
            vertexObjects[i] = null;
        }
    }
    
    /// <summary>
    /// 重新连接所有子物体
    /// </summary>
    public void ReconnectChildObjects()
    {
        List<GameObject> children = new List<GameObject>();
        
        // 收集所有子物体
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        
        // 清空列表并重新连接
        ClearAllObjects();
        
        // 添加子物体到列表（最多8个）
        for (int i = 0; i < Mathf.Min(children.Count, 8); i++)
        {
            if (i < vertexObjects.Count)
            {
                vertexObjects[i] = children[i];
            }
        }
        
        UpdateObjectsPositions();
    }
    
    /// <summary>
    /// 获取指定顶点的世界位置
    /// </summary>
    public Vector3 GetVertexWorldPosition(int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= 8 || targetCube == null)
            return Vector3.zero;
            
        Vector3[] vertices = targetCube.GetWorldVertices();
        if (vertexIndex < vertices.Length)
            return vertices[vertexIndex];
            
        return Vector3.zero;
    }
    
    /// <summary>
    /// 获取所有顶点的世界位置
    /// </summary>
    public Vector3[] GetAllVertexWorldPositions()
    {
        if (targetCube == null)
            return new Vector3[8];
            
        return targetCube.GetWorldVertices();
    }
    
    /// <summary>
    /// 获取顶点物体列表
    /// </summary>
    public List<GameObject> GetVertexObjects()
    {
        return vertexObjects;
    }
    
    /// <summary>
    /// 设置目标立方体
    /// </summary>
    public void SetTargetCube(BossRotationCube cube)
    {
        targetCube = cube;
        if (targetCube != null)
        {
            UpdateObjectsPositions();
        }
    }
    
    /// <summary>
    /// 启用/禁用跟随
    /// </summary>
    public void SetFollowEnabled(bool enabled)
    {
        followInFixedUpdate = enabled;
        followInUpdate = !enabled;
        
        if (enabled && targetCube != null)
        {
            UpdateObjectsPositions();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // 绘制顶点位置
        if (targetCube != null)
        {
            Vector3[] vertices = GetAllVertexWorldPositions();
            
            Gizmos.color = Color.green;
            for (int i = 0; i < vertices.Length; i++)
            {
                Gizmos.DrawSphere(vertices[i], 0.05f);
                
                // 绘制连接线到对应的物体
                if (i < vertexObjects.Count && vertexObjects[i] != null)
                {
                    Gizmos.DrawLine(vertices[i], vertexObjects[i].transform.position);
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showVertexLabels) return;
        
        // 绘制顶点标签
        if (targetCube != null)
        {
            Vector3[] vertices = GetAllVertexWorldPositions();
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = labelColor;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // 使用Handles.Label在场景中绘制文本
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(vertices[i], $"V{i}\n{vertexNames[i]}", style);
                #endif
            }
        }
    }
    
    /// <summary>
    /// 编辑器工具类（在Inspector中添加按钮）
    /// </summary>
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BossCubeFollower))]
    public class BossCubeFollowerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            BossCubeFollower follower = (BossCubeFollower)target;
            
            GUILayout.Space(10);
            GUILayout.Label("工具", UnityEditor.EditorStyles.boldLabel);
            
            if (GUILayout.Button("创建默认物体"))
            {
                follower.CreateAndSetDefaultObjects();
            }
            
            if (GUILayout.Button("清空所有物体"))
            {
                follower.ClearAllObjects();
            }
            
            if (GUILayout.Button("重新连接子物体"))
            {
                follower.ReconnectChildObjects();
            }
            
            if (GUILayout.Button("立即更新位置"))
            {
                follower.UpdateObjectsPositions();
            }
        }
    }
    #endif
}