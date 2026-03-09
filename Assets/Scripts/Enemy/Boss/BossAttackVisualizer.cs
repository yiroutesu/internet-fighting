using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(SimpleBossAI))]
public class BossAttackVisualizer : MonoBehaviour
{
    [Header("旋转立方体引用")]
    [SerializeField] private BossRotationCube rotationCube;
    
    [Header("攻击旋转设置")]
    [SerializeField] private AttackRotationSettings[] attackSettings;
    
    [Header("默认旋转")]
    [SerializeField] private bool restoreDefaultAfterAttack = true;
    [SerializeField] private float restoreDelay = 1f;
    
    [Header("立方体大小设置")]
    [SerializeField] private bool adjustCubeSize = true;
    [SerializeField] private float cubeSizeMultiplier = 1.5f;
    
    [Header("顶点变形效果")]
    [SerializeField] private bool enableVertexDeformation = true;
    [SerializeField] private VertexDeformationSettings dashVertexSettings;
    [SerializeField] private VertexDeformationSettings laserVertexSettings;
    
    [Header("攻击效果参数")]
    [SerializeField] private float dashFrontScale = 0.5f;    // Dash前面顶点缩小倍数
    [SerializeField] private float dashBackScale = 1.5f;     // Dash后面顶点拉长倍数
    [SerializeField] private float laserFrontScale = 0.7f;   // 激光前面顶点缩小倍数
    [SerializeField] private float meleeScale = 1.8f;        // 近战全部变大倍数
    [SerializeField] private float sweepRotationSpeed = 1200f; // 扫射旋转速度（度/秒）
    
    [Header("效果持续时间")]
    [SerializeField] private float dashDeformationDuration = 0.3f;
    [SerializeField] private float laserDeformationDuration = 0.2f;
    [SerializeField] private float meleeScaleDuration = 0.2f;
    [SerializeField] private float sweepRotationDuration = 0.3f;

    private SimpleBossAI bossAI;
    private Coroutine currentRotationRoutine;
    private Coroutine currentDeformationRoutine;
    private Coroutine currentSweepRoutine;
    private float originalCubeSize;
    private int[] frontVertexIndices = new int[] { 2,3,6,7}; // 前面四个顶点
    private int[] backVertexIndices = new int[] { 0,1,4,5};  // 后面四个顶点
    
    [System.Serializable]
    public class AttackRotationSettings
    {
        public enum AttackType { Dash, Melee, Laser, SweepLaser }
        public AttackType attackType;
        
        [Header("旋转参数")]
        public Vector3 rotationAngles = new Vector3(180f, 180f, 180f);
        public float rotationDuration = 0.5f;
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool useRandomRotation = true;
        
        [Header("大小效果")]
        public float sizeMultiplier = 1f;
        public float sizeEffectDuration = 0.2f;
        
        [Header("颜色反馈")]
        public Color rotationColor = Color.white;
        public float colorDuration = 0.3f;
    }
    
    [System.Serializable]
    public class VertexDeformationSettings
    {
        [Header("动画曲线")]
        public AnimationCurve deformationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float holdDuration = 0.2f;
    }
    
    void Start()
    {
        bossAI = GetComponent<SimpleBossAI>();
        
        if (rotationCube == null)
        {
            rotationCube = FindObjectOfType<BossRotationCube>();
            if (rotationCube == null)
            {
                Debug.LogWarning("未找到BossRotationCube组件");
                enabled = false;
                return;
            }
        }
        
        // 保存原始立方体大小
        originalCubeSize = rotationCube.CubeSize;
        
        // 如果需要调整大小，应用倍数
        if (adjustCubeSize && rotationCube != null)
        {
            rotationCube.SetCubeSize(originalCubeSize * cubeSizeMultiplier);
        }
        
        // 确保顶点变形启用
        if (enableVertexDeformation)
        {
            rotationCube.EnableVertexDeformation = true;
        }
        
        // 订阅攻击事件
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (bossAI == null) return;
        
        // 订阅所有攻击事件
        bossAI.onDashStart.AddListener(OnDashAttack);
        bossAI.onMeleeStart.AddListener(OnMeleeAttack);
        bossAI.onLaserStart.AddListener(OnLaserAttack);
        bossAI.onSweepLaserStart.AddListener(OnSweepLaserAttack);
    }
    
    #region 攻击事件处理
    
    private void OnDashAttack(Vector3 direction)
    {
        // Dash攻击：前面四个方块缩小，后面四个方块拉长，绕Z轴旋转
        ApplyDashDeformation(direction);
        PlayAttackRotation(AttackRotationSettings.AttackType.Dash, direction);
    }
    
    private void OnMeleeAttack()
    {
        // 近战攻击：全部变大
        ApplyMeleeScale();
        PlayAttackRotation(AttackRotationSettings.AttackType.Melee, Vector3.zero);
    }
    
    private void OnLaserAttack(Vector3 direction)
    {
        // 单发激光：和Dash相同，但不拉长后面的方块
        ApplyLaserDeformation(direction);
        PlayAttackRotation(AttackRotationSettings.AttackType.Laser, direction);
    }
    
    private void OnSweepLaserAttack(Vector3 direction)
    {
        // 激光扫射：每发射一次快速转一圈
        StartSweepRotation(direction);
        PlayAttackRotation(AttackRotationSettings.AttackType.SweepLaser, direction);
    }
    
    #endregion
    
    /// <summary>
    /// 应用Dash变形效果
    /// </summary>
    private void ApplyDashDeformation(Vector3 direction)
    {
        if (!enableVertexDeformation || rotationCube == null) return;
        
        // 停止之前的变形协程
        if (currentDeformationRoutine != null)
        {
            StopCoroutine(currentDeformationRoutine);
        }
        
        // 计算旋转轴（基于方向调整）
        Vector3 rotationAxis = Vector3.forward;
        if (direction != Vector3.zero)
        {
            // 根据攻击方向调整旋转轴
            rotationAxis = Quaternion.LookRotation(direction) * Vector3.forward;
        }
        
        // 使用BossRotationCube的快速变形方法
        rotationCube.QuickDeformForAttack(
            frontVertexIndices, 
            backVertexIndices,
            dashFrontScale,
            dashBackScale,
            rotationAxis,
            360f // 绕Z轴旋转360度
        );
        
        // 如果需要，可以在这里添加额外的效果协程
        currentDeformationRoutine = StartCoroutine(DeformationRecoveryRoutine(dashDeformationDuration));
    }
    
    /// <summary>
    /// 应用激光变形效果
    /// </summary>
    private void ApplyLaserDeformation(Vector3 direction)
    {
        if (!enableVertexDeformation || rotationCube == null) return;
        
        // 停止之前的变形协程
        if (currentDeformationRoutine != null)
        {
            StopCoroutine(currentDeformationRoutine);
        }
        
        // 计算旋转轴
        Vector3 rotationAxis = Vector3.forward;
        if (direction != Vector3.zero)
        {
            rotationAxis = Quaternion.LookRotation(direction) * Vector3.forward;
        }
        
        // 激光：前面缩小，后面不变（scale=1）
        rotationCube.QuickDeformForAttack(
            frontVertexIndices,
            null, // 后面顶点不变
            laserFrontScale,
            1.0f, // 后面不拉长
            rotationAxis,
            180f // 旋转180度
        );
        
        currentDeformationRoutine = StartCoroutine(DeformationRecoveryRoutine(laserDeformationDuration));
    }
    
    /// <summary>
    /// 应用近战缩放效果
    /// </summary>
    private void ApplyMeleeScale()
    {
        if (rotationCube == null) return;
        
        // 停止之前的变形协程
        if (currentDeformationRoutine != null)
        {
            StopCoroutine(currentDeformationRoutine);
        }
        
        // 使用快速缩放方法
        rotationCube.QuickScaleAll(meleeScale);
        
        currentDeformationRoutine = StartCoroutine(MeleeRecoveryRoutine());
    }
    
    /// <summary>
    /// 开始扫射旋转
    /// </summary>
    private void StartSweepRotation(Vector3 direction)
    {
        if (rotationCube == null) return;
        
        // 停止之前的扫射协程
        if (currentSweepRoutine != null)
        {
            StopCoroutine(currentSweepRoutine);
        }
        
        currentSweepRoutine = StartCoroutine(SweepRotationRoutine(direction));
    }
    
    /// <summary>
    /// 播放基本攻击旋转
    /// </summary>
    private void PlayAttackRotation(AttackRotationSettings.AttackType attackType, Vector3 direction)
    {
        if (rotationCube == null) return;
        
        // 查找对应的攻击设置
        AttackRotationSettings settings = GetSettingsForAttack(attackType);
        if (settings == null)
        {
            Debug.LogWarning($"未找到{attackType}的攻击设置");
            return;
        }
        
        // 开始旋转动画
        if (currentRotationRoutine != null)
        {
            StopCoroutine(currentRotationRoutine);
        }
        
        currentRotationRoutine = StartCoroutine(AttackRotationRoutine(settings, direction));
    }
    
    private AttackRotationSettings GetSettingsForAttack(AttackRotationSettings.AttackType attackType)
    {
        foreach (var setting in attackSettings)
        {
            if (setting.attackType == attackType)
                return setting;
        }
        return null;
    }
    
    #region 协程方法
    
    /// <summary>
    /// 变形恢复协程
    /// </summary>
    private IEnumerator DeformationRecoveryRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        
        // 确保变形被恢复
        if (rotationCube != null && rotationCube.IsDeforming())
        {
            // BossRotationCube会自动恢复变形，这里只需等待
            while (rotationCube.IsDeforming())
            {
                yield return null;
            }
        }
        
        currentDeformationRoutine = null;
    }
    
    /// <summary>
    /// 近战恢复协程
    /// </summary>
    private IEnumerator MeleeRecoveryRoutine()
    {
        yield return new WaitForSeconds(meleeScaleDuration);
        
        // 等待变形恢复
        if (rotationCube != null && rotationCube.IsDeforming())
        {
            while (rotationCube.IsDeforming())
            {
                yield return null;
            }
        }
        
        currentDeformationRoutine = null;
    }
    
    /// <summary>
    /// 扫射旋转协程
    /// </summary>
    private IEnumerator SweepRotationRoutine(Vector3 direction)
    {
        Quaternion startRotation = rotationCube.CurrentRotation;
        
        // 根据扫射方向确定旋转轴
        Vector3 rotationAxis = Vector3.up; // 默认绕Y轴旋转
        if (direction != Vector3.zero)
        {
            // 可以根据扫射方向调整旋转轴
            rotationAxis = Vector3.Cross(Vector3.up, direction).normalized;
        }
        
        // 计算目标旋转角度（快速旋转360度）
        float targetAngle = 360f;
        float elapsed = 0f;
        
        while (elapsed < sweepRotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / sweepRotationDuration);
            
            // 计算当前角度
            float currentAngle = targetAngle * progress;
            
            // 应用旋转
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, rotationAxis);
            rotationCube.CurrentRotation = startRotation * rotation;
            
            yield return null;
        }
        
        // 确保完成完整旋转
        rotationCube.CurrentRotation = startRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        
        currentSweepRoutine = null;
    }
    
    /// <summary>
    /// 攻击旋转协程
    /// </summary>
    private IEnumerator AttackRotationRoutine(AttackRotationSettings settings, Vector3 direction)
    {
        if (rotationCube == null) yield break;
        
        // 保存当前状态
        Quaternion originalRotation = rotationCube.CurrentRotation;
        float currentCubeSize = rotationCube.CubeSize;
        
        // 计算目标旋转角度
        Vector3 targetRotationAngles = settings.rotationAngles;
        if (settings.useRandomRotation)
        {
            targetRotationAngles = new Vector3(
                Random.Range(-settings.rotationAngles.x, settings.rotationAngles.x),
                Random.Range(-settings.rotationAngles.y, settings.rotationAngles.y),
                Random.Range(-settings.rotationAngles.z, settings.rotationAngles.z)
            );
        }
        
        // 如果有方向参数，调整旋转
        if (direction != Vector3.zero)
        {
            Quaternion directionRotation = Quaternion.LookRotation(direction);
            targetRotationAngles += directionRotation.eulerAngles;
        }
        
        // 设置目标旋转
        Quaternion targetRotation = Quaternion.Euler(targetRotationAngles);
        
        // 执行旋转动画
        float elapsed = 0f;
        while (elapsed < settings.rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settings.rotationDuration);
            float curveT = settings.rotationCurve.Evaluate(t);
            
            // 应用旋转
            rotationCube.CurrentRotation = Quaternion.Slerp(originalRotation, targetRotation, curveT);
            
            yield return null;
        }
        
        // 确保到达目标
        rotationCube.CurrentRotation = targetRotation;
        
        // 恢复默认状态
        if (restoreDefaultAfterAttack)
        {
            yield return new WaitForSeconds(restoreDelay);
            
            elapsed = 0f;
            float restoreDuration = settings.rotationDuration * 0.5f;
            
            while (elapsed < restoreDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / restoreDuration);
                
                // 恢复旋转
                rotationCube.CurrentRotation = Quaternion.Slerp(targetRotation, originalRotation, t);
                
                yield return null;
            }
            
            // 确保完全恢复
            rotationCube.CurrentRotation = originalRotation;
        }
        
        currentRotationRoutine = null;
    }
    
    #endregion
    
    /// <summary>
    /// 设置顶点变形启用/禁用
    /// </summary>
    public void SetVertexDeformationEnabled(bool enabled)
    {
        enableVertexDeformation = enabled;
        if (rotationCube != null)
        {
            rotationCube.EnableVertexDeformation = enabled;
        }
    }
    
    /// <summary>
    /// 设置Dash变形参数
    /// </summary>
    public void SetDashDeformationParams(float frontScale, float backScale, float duration)
    {
        dashFrontScale = frontScale;
        dashBackScale = backScale;
        dashDeformationDuration = duration;
    }
    
    /// <summary>
    /// 设置激光变形参数
    /// </summary>
    public void SetLaserDeformationParams(float frontScale, float duration)
    {
        laserFrontScale = frontScale;
        laserDeformationDuration = duration;
    }
    
    /// <summary>
    /// 设置近战缩放参数
    /// </summary>
    public void SetMeleeScaleParams(float scale, float duration)
    {
        meleeScale = scale;
        meleeScaleDuration = duration;
    }
    
    /// <summary>
    /// 设置扫射旋转参数
    /// </summary>
    public void SetSweepRotationParams(float speed, float duration)
    {
        sweepRotationSpeed = speed;
        sweepRotationDuration = duration;
    }
    
    /// <summary>
    /// 重置所有变形
    /// </summary>
    public void ResetAllDeformations()
    {
        if (rotationCube != null)
        {
            rotationCube.ResetDeformation();
        }
        
        if (currentDeformationRoutine != null)
        {
            StopCoroutine(currentDeformationRoutine);
            currentDeformationRoutine = null;
        }
        
        if (currentSweepRoutine != null)
        {
            StopCoroutine(currentSweepRoutine);
            currentSweepRoutine = null;
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // 如果游戏仍在运行，恢复原始大小和变形
        if (Application.isPlaying && rotationCube != null)
        {
            rotationCube.SetCubeSize(originalCubeSize);
            rotationCube.ResetDeformation();
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (bossAI == null) return;
        
        bossAI.onDashStart.RemoveListener(OnDashAttack);
        bossAI.onMeleeStart.RemoveListener(OnMeleeAttack);
        bossAI.onLaserStart.RemoveListener(OnLaserAttack);
        bossAI.onSweepLaserStart.RemoveListener(OnSweepLaserAttack);
    }
    
    #if UNITY_EDITOR
    [ContextMenu("设置默认攻击配置")]
    private void SetupDefaultAttackSettings()
    {
        attackSettings = new AttackRotationSettings[4];
        
        // 冲刺攻击设置
        attackSettings[0] = new AttackRotationSettings
        {
            attackType = AttackRotationSettings.AttackType.Dash,
            rotationAngles = new Vector3(0f, 0f, 180f), // 绕Z轴旋转
            rotationDuration = 0.8f,
            rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            useRandomRotation = false,
            sizeMultiplier = 1f,
            sizeEffectDuration = 0f,
            rotationColor = Color.red,
        };
        
        // 近战攻击设置
        attackSettings[1] = new AttackRotationSettings
        {
            attackType = AttackRotationSettings.AttackType.Melee,
            rotationAngles = new Vector3(45f, 45f, 45f),
            rotationDuration = 0.3f,
            rotationCurve = AnimationCurve.Linear(0, 0, 1, 1),
            useRandomRotation = true,
            sizeMultiplier = 1f,
            sizeEffectDuration = 0f,
            rotationColor = Color.yellow,
        };
        
        // 激光攻击设置
        attackSettings[2] = new AttackRotationSettings
        {
            attackType = AttackRotationSettings.AttackType.Laser,
            rotationAngles = new Vector3(0f, 0f, 90f), // 绕Z轴旋转但幅度较小
            rotationDuration = 0.4f,
            rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            useRandomRotation = false,
            sizeMultiplier = 1f,
            sizeEffectDuration = 0f,
            rotationColor = Color.blue,
        };
        
        // 扫射激光设置
        attackSettings[3] = new AttackRotationSettings
        {
            attackType = AttackRotationSettings.AttackType.SweepLaser,
            rotationAngles = new Vector3(0f, 360f, 0f), // 快速转一圈
            rotationDuration = 0.3f,
            rotationCurve = AnimationCurve.Linear(0, 0, 1, 1),
            useRandomRotation = false,
            sizeMultiplier = 1f,
            sizeEffectDuration = 0f,
            rotationColor = new Color(1f, 0.5f, 0f),
        };
        
        // 设置默认顶点变形配置
        dashVertexSettings = new VertexDeformationSettings
        {
            deformationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            holdDuration = 0.2f
        };
        
        laserVertexSettings = new VertexDeformationSettings
        {
            deformationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            holdDuration = 0.1f
        };
        
        // 设置默认参数
        dashFrontScale = 0.5f;
        dashBackScale = 1.5f;
        laserFrontScale = 0.7f;
        meleeScale = 1.8f;
        sweepRotationSpeed = 1200f;
        
        dashDeformationDuration = 0.3f;
        laserDeformationDuration = 0.2f;
        meleeScaleDuration = 0.2f;
        sweepRotationDuration = 0.3f;
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
}