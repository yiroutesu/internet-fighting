using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteController : MonoBehaviour
{
    [System.Serializable]
    public class BlendShapeInfo
    {
        public string name;
        public int index;
        [HideInInspector] public float currentWeight;
    }

    [Header("Skinned Mesh Renderers")]
    [SerializeField] private SkinnedMeshRenderer[] smr;

    [Header("Weight Range Settings")]
    [SerializeField] private bool useNormalizedWeight = false;
    [SerializeField] private float maxBlendShapeWeight = 100f;
    
    [Header("Hit Animation Settings")]
    [SerializeField] private AnimationCurve hitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float hitDuration = 0.2f;
    [SerializeField] private float hitHoldDuration = 0.3f;
    [SerializeField] private float hitFadeOutDuration = 0.1f;
    [SerializeField] private int hitBlendShapeIndex = 0;

    [Header("Blink Animation Settings")]
    [SerializeField] private AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private int blinkBlendShapeIndex = 1;

    [Header("Wake Up Animation Settings")]
    [SerializeField] private AnimationCurve wakeUpCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float wakeUpDuration = 3f;
    [SerializeField] private int wakeUpBlinkCount = 3;
    [SerializeField] private float wakeUpBlinkInterval = 0.5f;
    [SerializeField] private bool playWakeUpOnStart = true;
    [SerializeField] private int wakeUpBlendShapeIndex = 1;

    [Header("Auto Blink Settings")]
    [SerializeField] private bool enableAutoBlink = true;
    [SerializeField] private float minBlinkInterval = 2f;
    [SerializeField] private float maxBlinkInterval = 5f;

    private Coroutine hitCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine wakeUpCoroutine;
    private Coroutine autoBlinkCoroutine;
    private bool isHitPlaying = false;
    private bool isBlinking = false;
    private bool isWakingUp = false;

    // Start is called before the first frame update
    void Start()
    {
        if (smr == null || smr.Length == 0)
        {
            smr = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        // 自动检测权重范围
        if (!useNormalizedWeight)
        {
            DetectWeightRange();
        }

        // 初始化眨眼曲线示例 - 一个完整的眨眼周期（闭眼再睁开）
        if (blinkCurve.length == 2) // 默认只有两个关键帧
        {
            blinkCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 1), // 中间点完全闭眼
                new Keyframe(1, 0)     // 结束点睁眼
            );
        }

        // 初始化苏醒曲线示例 - 从闭眼逐渐睁开
        if (wakeUpCurve.length == 2 && wakeUpCurve.keys[0].value > wakeUpCurve.keys[1].value)
        {
            // 创建一个逐渐睁开眼的效果，可能带有轻微的抖动
            wakeUpCurve = new AnimationCurve(
                new Keyframe(0, 1),      // 开始时完全闭眼
                new Keyframe(0.2f, 0.9f), // 稍微睁开一点
                new Keyframe(0.4f, 0.95f), // 又稍微闭一点（模拟挣扎）
                new Keyframe(0.6f, 0.8f),  // 睁开更多
                new Keyframe(0.8f, 0.85f), // 轻微闭眼
                new Keyframe(1, 0)        // 完全睁开
            );
        }

        if (playWakeUpOnStart)
        {
            // 延迟一小段时间再开始苏醒动画，确保所有组件已初始化
            StartCoroutine(DelayedWakeUp(0.1f));
        }
        else if (enableAutoBlink)
        {
            StartAutoBlink();
        }
    }

    private void DetectWeightRange()
    {
        // 尝试检测模型的权重范围
        if (smr != null && smr.Length > 0 && smr[0] != null)
        {
            // 检查是否有任何blend shape
            if (smr[0].sharedMesh.blendShapeCount > 0)
            {
                // 设置一个测试权重并检查是否出错
                float testWeight = 100f;
                smr[0].SetBlendShapeWeight(0, testWeight);
                float actualWeight = smr[0].GetBlendShapeWeight(0);
                
                // 如果实际权重不等于设置值，可能使用的是归一化权重
                if (Mathf.Abs(actualWeight - testWeight) > 0.01f)
                {
                    // 使用归一化权重
                    useNormalizedWeight = true;
                    maxBlendShapeWeight = 1f;
                }
                else
                {
                    // 使用标准权重范围
                    maxBlendShapeWeight = 100f;
                }
                
                // 重置权重
                smr[0].SetBlendShapeWeight(0, 0);
            }
        }
    }

    private IEnumerator DelayedWakeUp(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayWakeUpAnimation();
    }

    // void OnValidate()
    // {
    //     // Ensure duration values are positive
    //     hitDuration = Mathf.Max(0.01f, hitDuration);
    //     hitHoldDuration = Mathf.Max(0, hitHoldDuration);
    //     hitFadeOutDuration = Mathf.Max(0.01f, hitFadeOutDuration);
    //     blinkDuration = Mathf.Max(0.01f, blinkDuration);
    //     wakeUpDuration = Mathf.Max(0.5f, wakeUpDuration);
    //     wakeUpBlinkCount = Mathf.Max(0, wakeUpBlinkCount);
    //     wakeUpBlinkInterval = Mathf.Max(0.1f, wakeUpBlinkInterval);
    //     minBlinkInterval = Mathf.Max(0.5f, minBlinkInterval);
    //     maxBlinkInterval = Mathf.Max(minBlinkInterval, maxBlinkInterval);
    //     maxBlendShapeWeight = Mathf.Max(0.1f, maxBlendShapeWeight);
    // }

    /// <summary>
    /// 外部调用此函数播放受击动画
    /// </summary>
    public void PlayHitAnimation()
    {
        // 打断眨眼动画
        if (isBlinking && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            SetBlendShapeWeight(blinkBlendShapeIndex, 0);
            isBlinking = false;
        }

        // 打断苏醒动画
        if (isWakingUp && wakeUpCoroutine != null)
        {
            StopCoroutine(wakeUpCoroutine);
            SetBlendShapeWeight(wakeUpBlendShapeIndex, 0);
            isWakingUp = false;
            
            // 苏醒被打断后，如果启用自动眨眼则开始
            if (enableAutoBlink)
            {
                StartAutoBlink();
            }
        }

        // 如果正在播放受击动画，重新开始
        if (isHitPlaying && hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }

        // 开始受击动画
        hitCoroutine = StartCoroutine(PlayHitAnimationRoutine());
    }

    private IEnumerator PlayHitAnimationRoutine()
    {
        isHitPlaying = true;
        
        // 阶段1: 播放受击动画曲线
        float elapsedTime = 0f;
        while (elapsedTime < hitDuration)
        {
            float normalizedTime = elapsedTime / hitDuration;
            float weight = hitCurve.Evaluate(normalizedTime) * maxBlendShapeWeight;
            SetBlendShapeWeight(hitBlendShapeIndex, weight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保到达曲线终点
        float peakWeight = hitCurve.Evaluate(1f) * maxBlendShapeWeight;
        SetBlendShapeWeight(hitBlendShapeIndex, peakWeight);
        
        // 阶段2: 保持受击表情
        yield return new WaitForSeconds(hitHoldDuration);
        
        // 阶段3: 淡出受击表情
        elapsedTime = 0f;
        while (elapsedTime < hitFadeOutDuration)
        {
            float weight = Mathf.Lerp(peakWeight, 0, elapsedTime / hitFadeOutDuration);
            SetBlendShapeWeight(hitBlendShapeIndex, weight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        SetBlendShapeWeight(hitBlendShapeIndex, 0);
        isHitPlaying = false;
    }

    /// <summary>
    /// 播放苏醒动画
    /// </summary>
    public void PlayWakeUpAnimation()
    {
        if (isWakingUp && wakeUpCoroutine != null)
        {
            StopCoroutine(wakeUpCoroutine);
        }
        
        // 停止自动眨眼
        if (autoBlinkCoroutine != null)
        {
            StopCoroutine(autoBlinkCoroutine);
        }
        
        wakeUpCoroutine = StartCoroutine(PlayWakeUpRoutine());
    }

    private IEnumerator PlayWakeUpRoutine()
    {
        isWakingUp = true;
        
        // 开始时确保眼睛是闭着的
        SetBlendShapeWeight(wakeUpBlendShapeIndex, maxBlendShapeWeight);
        
        // 阶段1: 缓慢睁开眼（主要苏醒过程）
        float elapsedTime = 0f;
        while (elapsedTime < wakeUpDuration)
        {
            float normalizedTime = elapsedTime / wakeUpDuration;
            float weight = wakeUpCurve.Evaluate(normalizedTime) * maxBlendShapeWeight;
            SetBlendShapeWeight(wakeUpBlendShapeIndex, weight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保完全睁开眼
        SetBlendShapeWeight(wakeUpBlendShapeIndex, 0);
        
        // 阶段2: 苏醒后的几次眨眼（模拟适应光线）
        for (int i = 0; i < wakeUpBlinkCount; i++)
        {
            // 播放一次眨眼
            PlaySingleBlink();
            
            // 等待眨眼完成
            while (isBlinking)
            {
                yield return null;
            }
            
            // 如果不是最后一次眨眼，等待间隔
            if (i < wakeUpBlinkCount - 1)
            {
                yield return new WaitForSeconds(wakeUpBlinkInterval);
            }
        }
        
        isWakingUp = false;
        
        // 苏醒完成后，如果启用自动眨眼则开始
        if (enableAutoBlink)
        {
            StartAutoBlink();
        }
    }

    /// <summary>
    /// 播放单次眨眼动画
    /// </summary>
    public void PlayBlink()
    {
        if (isBlinking && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(PlayBlinkRoutine());
    }
    
    /// <summary>
    /// 内部使用的单次眨眼（不会打断苏醒动画）
    /// </summary>
    private void PlaySingleBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(PlayBlinkRoutine());
    }

    private IEnumerator PlayBlinkRoutine()
    {
        isBlinking = true;
        float elapsedTime = 0f;

        // 直接使用曲线控制完整的眨眼周期
        while (elapsedTime < blinkDuration)
        {
            float normalizedTime = elapsedTime / blinkDuration;
            float weight = blinkCurve.Evaluate(normalizedTime) * maxBlendShapeWeight;
            SetBlendShapeWeight(blinkBlendShapeIndex, weight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 确保动画结束在正确的位置
        SetBlendShapeWeight(blinkBlendShapeIndex, blinkCurve.Evaluate(1f) * maxBlendShapeWeight);
        isBlinking = false;
    }

    /// <summary>
    /// 开始自动眨眼
    /// </summary>
    private void StartAutoBlink()
    {
        if (autoBlinkCoroutine != null)
        {
            StopCoroutine(autoBlinkCoroutine);
        }
        autoBlinkCoroutine = StartCoroutine(AutoBlinkRoutine());
    }

    private IEnumerator AutoBlinkRoutine()
    {
        while (enableAutoBlink && !isWakingUp) // 苏醒时不自动眨眼
        {
            // 等待随机间隔
            float blinkInterval = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(blinkInterval);

            // 如果没有在播放受击动画，则眨眼
            if (!isHitPlaying && !isWakingUp)
            {
                PlayBlink();
            }

            // 等待眨眼完成
            while (isBlinking)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// 设置所有SkinnedMeshRenderer的BlendShape权重
    /// </summary>
    private void SetBlendShapeWeight(int blendShapeIndex, float weight)
    {
        if (smr == null) return;

        // 确保权重不超过最大值
        weight = Mathf.Clamp(weight, 0, maxBlendShapeWeight);
        
        foreach (var renderer in smr)
        {
            if (renderer != null && blendShapeIndex >= 0 && blendShapeIndex < renderer.sharedMesh.blendShapeCount)
            {
                renderer.SetBlendShapeWeight(blendShapeIndex, weight);
            }
        }
    }

    /// <summary>
    /// 手动设置权重范围
    /// </summary>
    public void SetWeightRange(bool normalized, float maxWeight = 100f)
    {
        useNormalizedWeight = normalized;
        maxBlendShapeWeight = maxWeight;
    }

    /// <summary>
    /// 获取当前权重最大值
    /// </summary>
    public float GetMaxWeight()
    {
        return maxBlendShapeWeight;
    }

    /// <summary>
    /// 启用/禁用自动眨眼
    /// </summary>
    public void SetAutoBlinkEnabled(bool enabled)
    {
        enableAutoBlink = enabled;
        if (enabled && !isWakingUp)
        {
            StartAutoBlink();
        }
        else if (autoBlinkCoroutine != null)
        {
            StopCoroutine(autoBlinkCoroutine);
        }
    }

    /// <summary>
    /// 立即停止所有动画
    /// </summary>
    public void StopAllAnimations()
    {
        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            isHitPlaying = false;
        }
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            isBlinking = false;
        }
        if (wakeUpCoroutine != null)
        {
            StopCoroutine(wakeUpCoroutine);
            isWakingUp = false;
        }
        if (autoBlinkCoroutine != null)
        {
            StopCoroutine(autoBlinkCoroutine);
        }

        // 重置所有BlendShape权重
        SetBlendShapeWeight(hitBlendShapeIndex, 0);
        SetBlendShapeWeight(blinkBlendShapeIndex, 0);
        SetBlendShapeWeight(wakeUpBlendShapeIndex, 0);
    }

    /// <summary>
    /// 重置所有表情为初始状态
    /// </summary>
    public void ResetAllExpressions()
    {
        StopAllAnimations();
        
        // 重置所有blend shapes到0
        if (smr != null)
        {
            foreach (var renderer in smr)
            {
                if (renderer != null)
                {
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        renderer.SetBlendShapeWeight(i, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 测试权重范围
    /// </summary>
    public void TestWeightRange()
    {
        Debug.Log("当前最大权重值: " + maxBlendShapeWeight);
        Debug.Log("是否使用归一化权重: " + useNormalizedWeight);
        
        if (smr != null && smr.Length > 0 && smr[0] != null)
        {
            if (smr[0].sharedMesh.blendShapeCount > 0)
            {
                Debug.Log("模型包含 " + smr[0].sharedMesh.blendShapeCount + " 个blend shapes");
                
                // 测试设置不同的权重值
                float[] testWeights = { 0.5f, 1f, 50f, 100f };
                foreach (float testWeight in testWeights)
                {
                    smr[0].SetBlendShapeWeight(0, testWeight);
                    float actualWeight = smr[0].GetBlendShapeWeight(0);
                    Debug.Log($"设置权重 {testWeight} -> 实际权重 {actualWeight}");
                    smr[0].SetBlendShapeWeight(0, 0);
                }
            }
        }
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }
}