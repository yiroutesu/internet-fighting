using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
[ExecuteAlways]
public class LaserController : MonoBehaviour
{
    // 引用
    private Renderer laserRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    // 激光状态
    [Header("基础设置")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private float maxLength = 10f;
    
    [Header("动态控制")]
    [Range(0f, 1f)] public float intensityMultiplier = 1f;
    [Range(0f, 2f)] public float pulseSpeed = 1f;
    [Range(0f, 1f)] public float distortionAmount = 0.1f;
    
    [Header("颜色控制")]
    public Color laserColor = Color.red;
    public Color coreColor = Color.white;
    [Range(0f, 5f)] public float glowIntensity = 1f;
    
    [Header("效果控制")]
    public bool enableScanlines = true;
    public bool enablePulsing = true;
    public bool enableNoise = true;
    
    [Header("动画曲线")]
    public AnimationCurve widthOverTime = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve intensityOverTime = AnimationCurve.Linear(0, 1, 1, 1);
    
    // 私有变量
    private float currentTime = 0f;
    private float targetLength = 0f;
    private float currentLength = 0f;
    private Coroutine activationRoutine;
    
    void Awake()
    {
        laserRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        laserRenderer.GetPropertyBlock(propertyBlock);
        
        // 初始化长度
        targetLength = isActive ? maxLength : 0f;
        currentLength = targetLength;
    }
    
    void Update()
    {
        if (laserRenderer == null) return;
        
        currentTime += Time.deltaTime;
        
        // 平滑过渡长度
        currentLength = Mathf.Lerp(currentLength, targetLength, Time.deltaTime * 10f);
        
        // 更新材质属性
        UpdateMaterialProperties();
        
        // 更新激光位置和缩放
        UpdateLaserTransform();
    }
    
    void UpdateMaterialProperties()
    {
        laserRenderer.GetPropertyBlock(propertyBlock);
        
        // 基础属性
        propertyBlock.SetColor("_BaseColor", laserColor);
        propertyBlock.SetColor("_CoreColor", coreColor);
        propertyBlock.SetFloat("_Intensity", intensityMultiplier * intensityOverTime.Evaluate(currentTime));
        
        // 动态效果
        propertyBlock.SetFloat("_PulseSpeed", enablePulsing ? pulseSpeed : 0f);
        propertyBlock.SetFloat("_ScanlineSpeed", enableScanlines ? 3f : 0f);
        propertyBlock.SetFloat("_NoiseAmount", enableNoise ? 0.1f : 0f);
        propertyBlock.SetFloat("_Distortion", distortionAmount);
        propertyBlock.SetFloat("_GlowIntensity", glowIntensity);
        
        // 宽度动画
        float width = widthOverTime.Evaluate(currentTime);
        propertyBlock.SetFloat("_Width", width * 0.1f);
        
        laserRenderer.SetPropertyBlock(propertyBlock);
    }
    
    void UpdateLaserTransform()
    {
        // 调整激光长度
        transform.localScale = new Vector3(1, 1, currentLength);
    }
    
    #region 公共控制方法
    
    /// <summary>
    /// 激活/关闭激光
    /// </summary>
    public void SetActive(bool active, float duration = 0.5f)
    {
        isActive = active;
        targetLength = active ? maxLength : 0f;
        
        if (activationRoutine != null)
            StopCoroutine(activationRoutine);
        
        if (gameObject.activeInHierarchy)
            activationRoutine = StartCoroutine(ActivationRoutine(active, duration));
    }
    
    /// <summary>
    /// 设置激光颜色
    /// </summary>
    public void SetColor(Color color, Color? coreColor = null, float transitionTime = 0.3f)
    {
        StartCoroutine(TransitionColorRoutine(laserColor, color, 
            coreColor.HasValue ? coreColor.Value : this.coreColor, transitionTime));
    }
    
    /// <summary>
    /// 设置激光长度
    /// </summary>
    public void SetLength(float length, float transitionTime = 0.3f)
    {
        maxLength = Mathf.Clamp(length, 0.1f, 100f);
        targetLength = isActive ? maxLength : 0f;
    }
    
    /// <summary>
    /// 触发冲击波效果
    /// </summary>
    public void TriggerShockwave(float intensity = 2f, float duration = 0.5f)
    {
        StartCoroutine(ShockwaveRoutine(intensity, duration));
    }
    
    /// <summary>
    /// 设置随机噪波
    /// </summary>
    public void SetRandomNoise(float min = 0.05f, float max = 0.2f)
    {
        float noiseAmount = Random.Range(min, max);
        propertyBlock.SetFloat("_NoiseAmount", noiseAmount);
        laserRenderer.SetPropertyBlock(propertyBlock);
    }
    
    #endregion
    
    #region 协程
    
    IEnumerator ActivationRoutine(bool activating, float duration)
    {
        float elapsed = 0f;
        float startLength = currentLength;
        float endLength = activating ? maxLength : 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用缓动函数
            t = Mathf.SmoothStep(0, 1, t);
            currentLength = Mathf.Lerp(startLength, endLength, t);
            
            // 激活时增加强度
            if (activating)
            {
                intensityMultiplier = Mathf.Lerp(0, 1, t);
            }
            
            yield return null;
        }
        
        currentLength = endLength;
        intensityMultiplier = activating ? 1f : 0f;
    }
    
    IEnumerator TransitionColorRoutine(Color fromColor, Color toColor, Color toCoreColor, float duration)
    {
        float elapsed = 0f;
        Color startCoreColor = coreColor;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            laserColor = Color.Lerp(fromColor, toColor, t);
            coreColor = Color.Lerp(startCoreColor, toCoreColor, t);
            
            yield return null;
        }
        
        laserColor = toColor;
        coreColor = toCoreColor;
    }
    
    IEnumerator ShockwaveRoutine(float intensity, float duration)
    {
        float originalIntensity = intensityMultiplier;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 冲击波效果：先强后弱
            intensityMultiplier = originalIntensity + 
                Mathf.Sin(t * Mathf.PI) * intensity;
            
            // 增加扭曲效果
            distortionAmount = Mathf.Lerp(0.3f, 0f, t);
            
            yield return null;
        }
        
        intensityMultiplier = originalIntensity;
        distortionAmount = 0.1f;
    }
    
    #endregion
    
    #region 编辑器方法
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        if (laserRenderer == null)
            laserRenderer = GetComponent<Renderer>();
        
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
        
        UpdateMaterialProperties();
    }
    #endif
    
    #endregion
}