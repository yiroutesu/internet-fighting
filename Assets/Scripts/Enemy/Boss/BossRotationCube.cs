using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BossRotationCube : MonoBehaviour
{
    [Header("立方体参数")]
    [Range(0.1f, 10f)]
    [SerializeField] private float cubeSize = 1f;
    
    [Header("旋转参数")]
    [Range(0.1f, 30f)]
    [SerializeField] private float rotationDuration = 10f; // 旋转持续时间
    [SerializeField] private Vector3 maxRotationAngles = new Vector3(360f, 360f, 360f); // 最大旋转角度范围
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1); // 旋转曲线
    [SerializeField] private bool useRelativeRotation = false; // 是否使用相对旋转（相对于当前位置）
    
    [Header("顶点变形参数")]
    [SerializeField] private bool enableVertexDeformation = false; // 是否启用顶点变形
    [SerializeField] private Vector3[] vertexOffsets = new Vector3[8]; // 顶点偏移量
    [SerializeField] private AnimationCurve deformationCurve = AnimationCurve.Linear(0, 0, 1, 1); // 变形曲线
    [SerializeField] private float deformationDuration = 0.5f; // 变形持续时间
    [SerializeField] private bool autoRecoverDeformation = true; // 是否自动恢复变形
    [SerializeField] private float deformationRecoveryDuration = 0.3f; // 变形恢复时间
    
    [Header("调试选项")]
    [SerializeField] private bool autoStart = true; // 是否自动开始
    [SerializeField] private bool loopRotation = true; // 是否循环旋转
    [SerializeField] private bool showRotationPath = true; // 显示旋转路径
    [SerializeField] private bool showDeformationVectors = false; // 显示变形向量
    
    [Header("显示设置")]
    [SerializeField] private bool showVertices = true;
    [SerializeField] private bool showEdges = true;
    [SerializeField] private Color vertexColor = Color.red;
    [SerializeField] private Color edgeColor = Color.blue;
    [SerializeField] private float vertexSize = 0.1f;
    [SerializeField] private Color deformationVectorColor = Color.green; // 变形向量颜色

    // 存储八个顶点
    private Vector3[] originalVertices = new Vector3[8];
    private Vector3[] currentVertices = new Vector3[8];
    private Vector3[] deformedVertices = new Vector3[8]; // 变形后的顶点
    
    // 旋转状态
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private Quaternion currentRotation;
    private Coroutine rotationCoroutine;
    private bool isRotating = false;
    
    // 变形状态
    private Coroutine deformationCoroutine;
    private bool isDeforming = false;
    private float deformationProgress = 0f;
    
    // 旋转时间
    private float rotationTimer = 0f;
    private float currentRotationProgress = 0f;
    
    // 边连接关系
    private readonly int[,] edges = {
        {0, 1}, {1, 2}, {2, 3}, {3, 0}, // 底面
        {4, 5}, {5, 6}, {6, 7}, {7, 4}, // 顶面
        {0, 4}, {1, 5}, {2, 6}, {3, 7}  // 侧面
    };

    void Start()
    {
        InitializeCubeVertices();
        currentRotation = Quaternion.identity;
        
        // 初始化变形顶点
        ResetDeformation();
        
        if (autoStart && Application.isPlaying)
        {
            StartNewRotation();
        }
    }

    void Update()
    {
        UpdateVertices();
        
        // 在编辑模式下更新显示
        if (!Application.isPlaying)
        {
            UpdateVertices();
        }
    }

    void OnValidate()
    {
        if (originalVertices != null && originalVertices.Length == 8)
        {
            InitializeCubeVertices();
            UpdateVertices();
        }
    }

    /// <summary>
    /// 初始化立方体的八个顶点
    /// </summary>
    private void InitializeCubeVertices()
    {
        float halfSize = cubeSize * 0.5f;
        
        originalVertices[0] = new Vector3(-halfSize, -halfSize, -halfSize);
        originalVertices[1] = new Vector3(halfSize, -halfSize, -halfSize);
        originalVertices[2] = new Vector3(halfSize, -halfSize, halfSize);
        originalVertices[3] = new Vector3(-halfSize, -halfSize, halfSize);
        originalVertices[4] = new Vector3(-halfSize, halfSize, -halfSize);
        originalVertices[5] = new Vector3(halfSize, halfSize, -halfSize);
        originalVertices[6] = new Vector3(halfSize, halfSize, halfSize);
        originalVertices[7] = new Vector3(-halfSize, halfSize, halfSize);
        
        // 重置变形顶点
        ResetDeformation();
    }

    /// <summary>
    /// 重置变形
    /// </summary>
    public void ResetDeformation()
    {
        for (int i = 0; i < 8; i++)
        {
            vertexOffsets[i] = Vector3.zero;
            deformedVertices[i] = originalVertices[i];
        }
        
        if (deformationCoroutine != null)
        {
            StopCoroutine(deformationCoroutine);
            deformationCoroutine = null;
        }
        
        isDeforming = false;
        deformationProgress = 0f;
        
        UpdateVertices();
    }

    /// <summary>
    /// 开始新的旋转
    /// </summary>
    public void StartNewRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        
        rotationCoroutine = StartCoroutine(RotationRoutine());
    }

    /// <summary>
    /// 停止旋转
    /// </summary>
    public void StopRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
        isRotating = false;
    }

    /// <summary>
    /// 重置到初始状态
    /// </summary>
    public void ResetToInitial()
    {
        StopRotation();
        ResetDeformation();
        currentRotation = Quaternion.identity;
        UpdateVertices();
    }

    /// <summary>
    /// 旋转协程
    /// </summary>
    private IEnumerator RotationRoutine()
    {
        isRotating = true;
        
        while (loopRotation)
        {
            // 设置起始和目标旋转
            startRotation = currentRotation;
            
            if (useRelativeRotation)
            {
                // 相对旋转：在当前基础上增加随机角度
                Vector3 randomRotation = new Vector3(
                    Random.Range(-maxRotationAngles.x, maxRotationAngles.x),
                    Random.Range(-maxRotationAngles.y, maxRotationAngles.y),
                    Random.Range(-maxRotationAngles.z, maxRotationAngles.z)
                );
                targetRotation = currentRotation * Quaternion.Euler(randomRotation);
            }
            else
            {
                // 绝对旋转：随机选择新角度
                Vector3 randomRotation = new Vector3(
                    Random.Range(-maxRotationAngles.x, maxRotationAngles.x),
                    Random.Range(-maxRotationAngles.y, maxRotationAngles.y),
                    Random.Range(-maxRotationAngles.z, maxRotationAngles.z)
                );
                targetRotation = Quaternion.Euler(randomRotation);
            }
            
            //Debug.Log($"开始新旋转: {startRotation.eulerAngles} -> {targetRotation.eulerAngles}, 持续时间: {rotationDuration}秒");
            
            // 执行旋转动画
            rotationTimer = 0f;
            
            while (rotationTimer < rotationDuration)
            {
                rotationTimer += Time.deltaTime;
                currentRotationProgress = Mathf.Clamp01(rotationTimer / rotationDuration);
                
                // 应用旋转曲线
                float t = rotationCurve.Evaluate(currentRotationProgress);
                
                // 球形插值
                currentRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                UpdateVertices();
                yield return null;
            }
            
            // 确保到达目标
            currentRotation = targetRotation;
            UpdateVertices();
            
            // 如果不是循环，则停止
            if (!loopRotation)
            {
                break;
            }
        }
        
        isRotating = false;
        rotationCoroutine = null;
        
        //Debug.Log("旋转完成");
    }

    /// <summary>
    /// 更新顶点位置（考虑变形）
    /// </summary>
    private void UpdateVertices()
    {
        if (enableVertexDeformation)
        {
            // 应用变形到顶点
            for (int i = 0; i < 8; i++)
            {
                deformedVertices[i] = originalVertices[i] + vertexOffsets[i];
            }
            
            // 应用旋转到变形后的顶点
            for (int i = 0; i < 8; i++)
            {
                currentVertices[i] = currentRotation * deformedVertices[i];
            }
        }
        else
        {
            // 直接应用旋转到原始顶点
            for (int i = 0; i < 8; i++)
            {
                currentVertices[i] = currentRotation * originalVertices[i];
            }
        }
    }

    /// <summary>
    /// 设置顶点偏移（直接设置）
    /// </summary>
    public void SetVertexOffset(int vertexIndex, Vector3 offset)
    {
        if (vertexIndex >= 0 && vertexIndex < 8)
        {
            vertexOffsets[vertexIndex] = offset;
            UpdateVertices();
        }
        else
        {
            Debug.LogError($"顶点索引{vertexIndex}超出范围(0-7)");
        }
    }

    /// <summary>
    /// 设置所有顶点偏移（直接设置）
    /// </summary>
    public void SetAllVertexOffsets(Vector3[] offsets)
    {
        if (offsets != null && offsets.Length == 8)
        {
            vertexOffsets = (Vector3[])offsets.Clone();
            UpdateVertices();
        }
        else
        {
            Debug.LogError("顶点偏移数组必须包含8个元素");
        }
    }

    /// <summary>
    /// 应用顶点变形动画
    /// </summary>
    public void ApplyVertexDeformation(Vector3[] targetOffsets, float duration = 0f)
    {
        if (targetOffsets == null || targetOffsets.Length != 8)
        {
            Debug.LogError("目标偏移数组必须包含8个元素");
            return;
        }
        
        if (deformationCoroutine != null)
        {
            StopCoroutine(deformationCoroutine);
        }
        
        float actualDuration = duration > 0 ? duration : deformationDuration;
        deformationCoroutine = StartCoroutine(DeformationRoutine(targetOffsets, actualDuration));
    }

    /// <summary>
    /// 顶点变形动画协程
    /// </summary>
    private IEnumerator DeformationRoutine(Vector3[] targetOffsets, float duration)
    {
        isDeforming = true;
        enableVertexDeformation = true;
        
        Vector3[] startOffsets = (Vector3[])vertexOffsets.Clone();
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            deformationProgress = Mathf.Clamp01(elapsed / duration);
            
            // 应用变形曲线
            float t = deformationCurve.Evaluate(deformationProgress);
            
            // 插值顶点偏移
            for (int i = 0; i < 8; i++)
            {
                vertexOffsets[i] = Vector3.Lerp(startOffsets[i], targetOffsets[i], t);
            }
            
            UpdateVertices();
            yield return null;
        }
        
        // 确保到达目标
        for (int i = 0; i < 8; i++)
        {
            vertexOffsets[i] = targetOffsets[i];
        }
        UpdateVertices();
        
        // 如果需要自动恢复
        if (autoRecoverDeformation)
        {
            yield return new WaitForSeconds(0.1f); // 短暂保持
            
            // 恢复变形
            elapsed = 0f;
            startOffsets = (Vector3[])vertexOffsets.Clone();
            
            while (elapsed < deformationRecoveryDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deformationRecoveryDuration);
                
                // 恢复顶点偏移
                for (int i = 0; i < 8; i++)
                {
                    vertexOffsets[i] = Vector3.Lerp(startOffsets[i], Vector3.zero, t);
                }
                
                UpdateVertices();
                yield return null;
            }
            
            // 重置变形
            ResetDeformation();
        }
        
        isDeforming = false;
        deformationCoroutine = null;
    }

    /// <summary>
    /// 快速变形效果（用于攻击）
    /// </summary>
    public void QuickDeformForAttack(int[] frontIndices, int[] backIndices, 
                                     float frontScale = 0.5f, float backScale = 1.5f, 
                                     Vector3 rotationAxis = default, float rotationAngle = 0f)
    {
        if (deformationCoroutine != null)
        {
            StopCoroutine(deformationCoroutine);
        }
        
        Vector3[] targetOffsets = new Vector3[8];
        
        // 计算旋转
        Quaternion rotation = rotationAngle != 0f ? 
            Quaternion.AngleAxis(rotationAngle, rotationAxis.normalized) : 
            Quaternion.identity;
        
        // 计算变形偏移
        for (int i = 0; i < 8; i++)
        {
            Vector3 originalVertex = originalVertices[i];
            
            // 应用旋转
            Vector3 rotatedVertex = rotation * originalVertex;
            
            // 应用缩放
            float scale = 1f;
            if (frontIndices != null && System.Array.IndexOf(frontIndices, i) >= 0)
            {
                scale = frontScale;
            }
            else if (backIndices != null && System.Array.IndexOf(backIndices, i) >= 0)
            {
                scale = backScale;
            }
            
            Vector3 scaledVertex = rotatedVertex * scale;
            
            // 计算偏移
            targetOffsets[i] = scaledVertex - originalVertex;
        }
        
        // 应用变形动画
        ApplyVertexDeformation(targetOffsets, 0.2f);
    }

    /// <summary>
    /// 快速缩放效果（用于近战攻击）
    /// </summary>
    public void QuickScaleAll(float scaleMultiplier)
    {
        if (deformationCoroutine != null)
        {
            StopCoroutine(deformationCoroutine);
        }
        
        Vector3[] targetOffsets = new Vector3[8];
        
        for (int i = 0; i < 8; i++)
        {
            targetOffsets[i] = originalVertices[i] * (scaleMultiplier - 1f);
        }
        
        // 应用变形动画
        ApplyVertexDeformation(targetOffsets, 0.2f);
    }

    /// <summary>
    /// 设置旋转持续时间
    /// </summary>
    public void SetRotationDuration(float duration)
    {
        rotationDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 设置立方体大小
    /// </summary>
    public void SetCubeSize(float size)
    {
        cubeSize = Mathf.Max(0.1f, size);
        InitializeCubeVertices();
        UpdateVertices();
    }

    /// <summary>
    /// 立即旋转到随机角度（不经过动画）
    /// </summary>
    public void SetRandomRotationImmediate()
    {
        Vector3 randomRotation = new Vector3(
            Random.Range(-maxRotationAngles.x, maxRotationAngles.x),
            Random.Range(-maxRotationAngles.y, maxRotationAngles.y),
            Random.Range(-maxRotationAngles.z, maxRotationAngles.z)
        );
        
        currentRotation = Quaternion.Euler(randomRotation);
        UpdateVertices();
    }

    /// <summary>
    /// 获取当前旋转进度（0-1）
    /// </summary>
    public float GetRotationProgress()
    {
        return currentRotationProgress;
    }

    /// <summary>
    /// 获取当前是否在旋转
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }

    /// <summary>
    /// 获取当前是否在变形
    /// </summary>
    public bool IsDeforming()
    {
        return isDeforming;
    }

    /// <summary>
    /// 获取当前旋转角度
    /// </summary>
    public Vector3 GetCurrentRotationEuler()
    {
        return currentRotation.eulerAngles;
    }

    /// <summary>
    /// 获取目标旋转角度
    /// </summary>
    public Vector3 GetTargetRotationEuler()
    {
        return targetRotation.eulerAngles;
    }

    /// <summary>
    /// 获取当前顶点偏移
    /// </summary>
    public Vector3[] GetCurrentVertexOffsets()
    {
        return (Vector3[])vertexOffsets.Clone();
    }

    /// <summary>
    /// 获取变形后的顶点位置（局部坐标）
    /// </summary>
    public Vector3[] GetDeformedVerticesLocal()
    {
        return (Vector3[])deformedVertices.Clone();
    }

    void OnDrawGizmos()
    {
        if (!showVertices && !showEdges && !showDeformationVectors) return;
        
        Vector3[] worldVertices = GetWorldVertices();
        
        // 绘制顶点
        if (showVertices)
        {
            Gizmos.color = vertexColor;
            for (int i = 0; i < 8; i++)
            {
                Gizmos.DrawSphere(worldVertices[i], vertexSize);
            }
        }
        
        // 绘制边
        if (showEdges)
        {
            Gizmos.color = edgeColor;
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                Gizmos.DrawLine(
                    worldVertices[edges[i, 0]],
                    worldVertices[edges[i, 1]]
                );
            }
        }
        
        // 绘制变形向量
        if (showDeformationVectors && enableVertexDeformation)
        {
            Vector3[] originalWorldVertices = GetOriginalWorldVertices();
            Gizmos.color = deformationVectorColor;
            
            for (int i = 0; i < 8; i++)
            {
                if (vertexOffsets[i] != Vector3.zero)
                {
                    Gizmos.DrawLine(originalWorldVertices[i], worldVertices[i]);
                    Gizmos.DrawSphere(worldVertices[i], vertexSize * 1.2f);
                }
            }
        }
        
        // 绘制旋转路径
        if (showRotationPath && isRotating)
        {
            DrawRotationPath();
        }
    }

    /// <summary>
    /// 绘制旋转路径
    /// </summary>
    private void DrawRotationPath()
    {
        Vector3 center = transform.position;
        
        // 绘制起始方向
        Gizmos.color = Color.green;
        Vector3 startDir = startRotation * Vector3.forward * cubeSize;
        Gizmos.DrawLine(center, center + startDir);
        Gizmos.DrawWireSphere(center + startDir, vertexSize * 0.5f);
        
        // 绘制目标方向
        Gizmos.color = Color.red;
        Vector3 targetDir = targetRotation * Vector3.forward * cubeSize;
        Gizmos.DrawLine(center, center + targetDir);
        Gizmos.DrawWireSphere(center + targetDir, vertexSize * 0.5f);
        
        // 绘制当前方向
        Gizmos.color = Color.yellow;
        Vector3 currentDir = currentRotation * Vector3.forward * cubeSize;
        Gizmos.DrawLine(center, center + currentDir);
        Gizmos.DrawWireSphere(center + currentDir, vertexSize * 0.7f);
        
        // 绘制旋转弧线
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        const int segments = 20;
        Vector3 prevPoint = center + startDir;
        
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Quaternion intermediateRot = Quaternion.Slerp(startRotation, targetRotation, t);
            Vector3 point = center + intermediateRot * Vector3.forward * cubeSize;
            
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(cubeSize, cubeSize, cubeSize));
    }

    /// <summary>
    /// 获取当前顶点的世界坐标
    /// </summary>
    public Vector3[] GetWorldVertices()
    {
        Vector3[] worldVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldVertices[i] = transform.TransformPoint(currentVertices[i]);
        }
        return worldVertices;
    }

    /// <summary>
    /// 获取原始顶点的世界坐标
    /// </summary>
    public Vector3[] GetOriginalWorldVertices()
    {
        Vector3[] worldVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldVertices[i] = transform.TransformPoint(originalVertices[i]);
        }
        return worldVertices;
    }

    /// <summary>
    /// 获取原始顶点的局部坐标
    /// </summary>
    public Vector3[] GetOriginalVertices()
    {
        return (Vector3[])originalVertices.Clone();
    }

    /// <summary>
    /// 获取当前顶点的局部坐标
    /// </summary>
    public Vector3[] GetCurrentVerticesLocal()
    {
        return (Vector3[])currentVertices.Clone();
    }

    // 属性访问器
    public float CubeSize
    {
        get { return cubeSize; }
        set { SetCubeSize(value); }
    }
    
    public float RotationDuration
    {
        get { return rotationDuration; }
        set { SetRotationDuration(value); }
    }
    
    public Quaternion CurrentRotation
    {
        get { return currentRotation; }
        set 
        { 
            currentRotation = value; 
            UpdateVertices();
        }
    }
    
    public bool EnableVertexDeformation
    {
        get { return enableVertexDeformation; }
        set 
        { 
            enableVertexDeformation = value; 
            if (!value) ResetDeformation();
            UpdateVertices();
        }
    }
    
    public float DeformationProgress
    {
        get { return deformationProgress; }
    }
}