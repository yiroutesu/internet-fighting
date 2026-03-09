using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SixLegIKController : MonoBehaviour
{
    static int nbLegs = 6;
    public int[] groupA = { 0, 2, 4 };
    public int[] groupB = { 1, 3, 5 };
    public Transform[] legTargets = new Transform[nbLegs];
    public float stepSize = 0.5f;
    public LayerMask groundLayer = ~0;   // 默认所有层
    public float smoothness = 8;
    public float stepHeight = 0.3f;
    public float raycastRange = 3;

    Vector3[] defaultLegPositions = new Vector3[nbLegs];
    Vector3[] lastLegPositions = new Vector3[nbLegs];
    public bool isLegMoving = true;//true:A ; false:B

    private Vector3 velocity;
    private Vector3 lastBodyPos;
    public int velocityMultiplier = 15;
    public float maxPush;
    private Vector3 lastBodyUp;
    private int movinglegs = 0;
    
    // 存储初始状态
    private Vector3 initialBodyPosition;
    private Quaternion initialBodyRotation;
    private Vector3[] initialLegWorldPositions = new Vector3[nbLegs];
    private bool isInitialized = false;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();
    
    // 添加延迟重置机制
    private bool needResetOnNextFrame = false;
    private int framesSinceEnable = 0;

    private void Awake()
    {
        // 取消事件监听
        // WaveManager.Instance.enemySpawner.OnEnemySpawnPositionReported.AddListener(Init);
    }

    private void OnEnable()
    {
        // 设置标志，在下一帧重置legTarget
        needResetOnNextFrame = true;
        framesSinceEnable = 0;
        
        // 重置移动状态
        movinglegs = 0;
        isLegMoving = true;
        velocity = Vector3.zero;
        lastBodyPos = transform.position;
        
        // 停止所有协程
        StopAllActiveCoroutines();
        
        // 如果已经初始化过，重置腿部位置
        if (isInitialized)
        {
            ResetLegTargetsToInitialState();
        }
    }
    
    private void OnDisable() 
    {
        // 禁用时停止所有协程
        StopAllActiveCoroutines();
        needResetOnNextFrame = false;
        framesSinceEnable = 0;
    }
    
    private void Start()
    {
        // 如果已经通过Init初始化，则不再重复初始化
        if (!isInitialized)
        {
            // 如果没有通过Init初始化，则尝试自动初始化
            AutoInitialize();
        }
    }
    
    /// <summary>
    /// 自动初始化（用于非对象池生成的蜘蛛）
    /// </summary>
    private void AutoInitialize()
    {
        if (isInitialized) return;
        
        // 使用当前位置进行初始化
        InitializeLegPositions();
        
        // 设置重置标志
        needResetOnNextFrame = true;
        framesSinceEnable = 0;
        
        // 重置移动状态
        movinglegs = 0;
        isLegMoving = true;
        velocity = Vector3.zero;
        lastBodyPos = transform.position;
        
        // 停止所有协程
        StopAllActiveCoroutines();
    }
    
    /// <summary>
    /// 公开的初始化函数，供生成池调用
    /// </summary>
    /// <param name="spawnPosition">生成位置</param>
    public void Init(Vector3 spawnPosition)
    {
        // 重置位置
        transform.position = spawnPosition;
        
        // 重新初始化腿部位置
        isInitialized = false;
        InitializeLegPositions();
        
        // 设置标志，在下一帧重置legTarget
        needResetOnNextFrame = true;
        framesSinceEnable = 0;
        
        // 重置移动状态
        movinglegs = 0;
        isLegMoving = true;
        velocity = Vector3.zero;
        lastBodyPos = transform.position;
        
        // 停止所有协程
        StopAllActiveCoroutines();
    }
    
    private void InitializeLegPositions()
    {
        if (isInitialized) return;
        
        // 确保legTargets已分配
        if (legTargets == null || legTargets.Length != nbLegs)
        {
            Debug.LogWarning("Leg targets not properly set up!");
            return;
        }
        
        // 记录初始身体状态
        initialBodyPosition = transform.position;
        initialBodyRotation = transform.rotation;
        
        // 记录初始腿部位置（世界坐标）
        for (int i = 0; i < nbLegs; ++i)
        {
            if (legTargets[i] != null)
            {
                // 确保legTarget当前位置有效
                if (Vector3.Distance(legTargets[i].position, transform.position) > raycastRange * 2)
                {
                    // 如果腿部位置太远，将其放置在本体附近
                    Vector3 offset = Vector3.zero;
                    switch (i)
                    {
                        case 0: offset = new Vector3(-stepSize, 0, stepSize); break;
                        case 1: offset = new Vector3(stepSize, 0, stepSize); break;
                        case 2: offset = new Vector3(-stepSize, 0, 0); break;
                        case 3: offset = new Vector3(stepSize, 0, 0); break;
                        case 4: offset = new Vector3(-stepSize, 0, -stepSize); break;
                        case 5: offset = new Vector3(stepSize, 0, -stepSize); break;
                    }
                    legTargets[i].position = transform.position + offset;
                }
                
                initialLegWorldPositions[i] = legTargets[i].position;
                // 计算相对于初始身体的局部位置
                defaultLegPositions[i] = Quaternion.Inverse(initialBodyRotation) * (initialLegWorldPositions[i] - initialBodyPosition);
                lastLegPositions[i] = legTargets[i].position;
            }
            else
            {
                Debug.LogError($"Leg target {i} is not assigned!");
                defaultLegPositions[i] = Vector3.zero;
                initialLegWorldPositions[i] = transform.position;
                lastLegPositions[i] = transform.position;
            }
        }
        
        lastBodyPos = transform.position;
        lastBodyUp = transform.up;
        isInitialized = true;
    }
    
    private void Update()
    {
        // 在Update中检查是否需要重置legTargets
        if (needResetOnNextFrame && framesSinceEnable >= 1)
        {
            // 父物体的transform应该在激活后的第一帧已经被设置
            ResetLegTargetsToInitialState();
            needResetOnNextFrame = false;
        }
        
        framesSinceEnable++;
    }
    
    private void ResetLegTargetsToInitialState()
    {
        if (!isInitialized) 
        {
            InitializeLegPositions();
            if (!isInitialized) return;
        }
        
        // 确保所有legTargets有效
        bool allTargetsValid = true;
        for (int i = 0; i < nbLegs; ++i)
        {
            if (legTargets[i] == null)
            {
                allTargetsValid = false;
                break;
            }
        }
        
        if (!allTargetsValid)
        {
            Debug.LogWarning("Some leg targets are null, cannot reset.");
            return;
        }
        
        // 将legTargets重置到初始状态（相对于初始身体位置）
        for (int i = 0; i < nbLegs; ++i)
        {
            if (legTargets[i] != null)
            {
                // 计算期望位置（基于当前身体状态）
                Vector3 desiredPosition = transform.position + transform.rotation * defaultLegPositions[i];
                
                // 使用射线检测获取地面信息
                Vector3[] positionAndNormalFwd = MatchToSurfaceFromAbove(desiredPosition, raycastRange, groundLayer);
                
                // 如果射线检测失败，使用期望位置
                if (positionAndNormalFwd[0] == desiredPosition && positionAndNormalFwd[1] == Vector3.zero)
                {
                    // 向下发射射线寻找地面
                    RaycastHit hit;
                    if (Physics.Raycast(desiredPosition + Vector3.up * raycastRange, Vector3.down, out hit, raycastRange * 2, groundLayer))
                    {
                        positionAndNormalFwd[0] = hit.point;
                    }
                }
                
                // 直接设置位置，不使用动画
                legTargets[i].position = positionAndNormalFwd[0];
                lastLegPositions[i] = legTargets[i].position;
            }
        }
        
        // 重置后立即更新固定不移动的腿
        for (int i = 0; i < 3; ++i)
        {
            int indexToMove = isLegMoving == false ? groupA[i] : groupB[i];
            if (legTargets[indexToMove] != null)
            {
                legTargets[indexToMove].position = lastLegPositions[indexToMove];
            }
        }
    }
    
    private void StopAllActiveCoroutines()
    {
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }
    
    Vector3[] MatchToSurfaceFromAbove(Vector3 point, float range, LayerMask layer)
    {
        Vector3 up = Vector3.up;
        Vector3[] res = new Vector3[2];
        // 计算射线起点（从上方开始）
        Vector3 rayStart = point + up * (range * 0.5f);
        Vector3 rayDirection = -up; // 向下发射
        
        // 使用Raycast进行射线检测
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, range, layer, QueryTriggerInteraction.Ignore))
        {
            res[0] = hit.point;
            res[1] = hit.normal;
        }
        else
        {
            res[0] = point;
            res[1] = Vector3.zero;
        }
        return res;
    }
    
    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        Vector3 startPos = lastLegPositions[index];
        movinglegs += 1; // 在开始移动时增加计数
        
        for (int i = 1; i <= smoothness; ++i)
        {
            if (legTargets[index] == null) yield break;
            
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (float)(smoothness + 1f));
            legTargets[index].position += transform.up * Mathf.Sin(i / (float)(smoothness + 1f) * Mathf.PI) * stepHeight;
            yield return new WaitForFixedUpdate();
        }
        
        if (legTargets[index] != null)
        {
            legTargets[index].position = targetPoint;
            lastLegPositions[index] = legTargets[index].position;
        }
        
        movinglegs -= 1;
    }
    
    public bool FindGroup(int number) => number switch
    {
        0 or 2 or 4 => true,  // GroupA
        1 or 3 or 5 => false,  // GroupB
    };
    
    private void BodyRotation()
    {
        // 根据腿部位置调整身体方向
        Vector3 v1 = legTargets[0].position - legTargets[5].position;
        Vector3 v2 = legTargets[3].position - legTargets[2].position;
        Vector3 normal = Vector3.Cross(v1, v2).normalized;
        Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));
        transform.up = up;
        
        // 保持与父物体的旋转关系
        if (transform.parent != null)
        {
            transform.rotation = Quaternion.LookRotation(-transform.parent.forward, up);
        }
        lastBodyUp = transform.up;
    }
    
    void FixedUpdate()
    {
        if (!isInitialized || !isActiveAndEnabled) return;
        
        // 如果有重置需求，先执行重置
        if (needResetOnNextFrame)
        {
            // 在FixedUpdate中也检查，确保即使Update被跳过也能重置
            ResetLegTargetsToInitialState();
            needResetOnNextFrame = false;
        }
        
        // 检查是否有legTarget为空
        bool allTargetsValid = true;
        for (int i = 0; i < nbLegs; ++i)
        {
            if (legTargets[i] == null)
            {
                allTargetsValid = false;
                break;
            }
        }
        
        if (!allTargetsValid) return;
        
        // 计算速度向量
        velocity = transform.position - lastBodyPos;
        
        // 计算每条腿的目标位置（基于当前身体状态）
        Vector3[] desiredPositions = new Vector3[nbLegs];
        int indexToMove;
        
        for (int i = 0; i < nbLegs; ++i)
        {
            // 使用相对于初始身体的局部位置计算当前位置
            desiredPositions[i] = transform.position + transform.rotation * defaultLegPositions[i];
        }
        
        // 固定不移动的腿
        for (int i = 0; i < 3; ++i)
        {
            indexToMove = isLegMoving == false ? groupA[i] : groupB[i];
            if (legTargets[indexToMove] != null)
            {
                legTargets[indexToMove].position = lastLegPositions[indexToMove];
            }
        }
        
        if (movinglegs == 0)
        {
            isLegMoving = !isLegMoving;
            for (int i = 0; i < 3; ++i)
            {
                indexToMove = isLegMoving == true ? groupA[i] : groupB[i];
                if (legTargets[indexToMove] == null) continue;
                
                float push = Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0f, maxPush);
                Vector3 targetPoint = desiredPositions[indexToMove]
                          + push * (desiredPositions[indexToMove] - legTargets[indexToMove].position)
                          + velocity.normalized * push;
                
                // 使用射线检测获取地面信息
                Vector3[] positionAndNormalFwd = MatchToSurfaceFromAbove(targetPoint, raycastRange, groundLayer);
                Coroutine coroutine = StartCoroutine(PerformStep(indexToMove, positionAndNormalFwd[0]));
                activeCoroutines.Add(coroutine);
            }
        }
        
        lastBodyPos = transform.position;
        // BodyRotation(); // 如果需要身体旋转，取消注释
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        for (int i = 0; i < nbLegs; ++i)
        {
            if (legTargets[i] == null) continue;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.5f);
            Gizmos.color = Color.green;
            // 显示期望位置
            Vector3 desiredPosition = transform.position + transform.rotation * defaultLegPositions[i];
            Gizmos.DrawWireSphere(desiredPosition, stepSize);
        }
    }
}