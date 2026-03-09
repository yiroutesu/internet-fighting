using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class AdvancedButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    public class HoverEffects
    {
        public bool enableScaleEffect = true;
        public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);
        public float scaleDuration = 0.15f;
        
        public bool enableBreathing = false;
        public float breathingAmount = 0.05f;
        public float breathingSpeed = 3f;
        
        public bool enableColorChange = true;
        public Color hoverColor = new Color(1f, 0.8f, 0.5f, 1f);
        public Color originalColor = Color.white;
        
        public bool enableShadowEffect = true;
        public Vector2 shadowDistance = new Vector2(5, 5);
    }
    
    [SerializeField] private HoverEffects hoverEffects = new HoverEffects();
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Image buttonImage;
    private TMP_Text buttonText;
    private Shadow shadowComponent;
    
    private bool isHovering = false;
    private Coroutine breathingCoroutine;
    private Coroutine scaleCoroutine;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TMP_Text>();
        shadowComponent = GetComponent<Shadow>();
        
        originalScale = rectTransform.localScale;
        
        if (buttonImage != null)
        {
            hoverEffects.originalColor = buttonImage.color;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        // 停止之前的动画
        StopAllCoroutines();
        
        AudioManager.instance?.Play("button_switch");
        // 缩放效果
        if (hoverEffects.enableScaleEffect)
        {
            scaleCoroutine = StartCoroutine(ScaleAnimation(
                originalScale, 
                new Vector3(
                    originalScale.x * hoverEffects.hoverScale.x,
                    originalScale.y * hoverEffects.hoverScale.y,
                    originalScale.z * hoverEffects.hoverScale.z
                ), 
                hoverEffects.scaleDuration
            ));
        }
        
        // 颜色变化
        if (hoverEffects.enableColorChange && buttonImage != null)
        {
            StartCoroutine(ColorAnimation(buttonImage.color, hoverEffects.hoverColor, hoverEffects.scaleDuration));
        }
        
        // 阴影效果
        if (hoverEffects.enableShadowEffect && shadowComponent != null)
        {
            StartCoroutine(ShadowAnimation(
                shadowComponent.effectDistance, 
                hoverEffects.shadowDistance, 
                hoverEffects.scaleDuration
            ));
        }
        
        // 呼吸效果
        if (hoverEffects.enableBreathing)
        {
            breathingCoroutine = StartCoroutine(BreathingAnimation());
        }
        
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        
        StopAllCoroutines();
        
        // 恢复所有效果
        if (hoverEffects.enableScaleEffect)
        {
            scaleCoroutine = StartCoroutine(ScaleAnimation(rectTransform.localScale, originalScale, hoverEffects.scaleDuration));
        }
        
        if (hoverEffects.enableColorChange && buttonImage != null)
        {
            StartCoroutine(ColorAnimation(buttonImage.color, hoverEffects.originalColor, hoverEffects.scaleDuration));
        }
        
        if (hoverEffects.enableShadowEffect && shadowComponent != null)
        {
            StartCoroutine(ShadowAnimation(shadowComponent.effectDistance, Vector2.zero, hoverEffects.scaleDuration));
        }
        
        breathingCoroutine = null;
    }
    
    private IEnumerator ScaleAnimation(Vector3 startScale, Vector3 targetScale, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            t = t * t * (3f - 2f * t); // 三次缓动
            
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            time += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = targetScale;
    }
    
    private IEnumerator ColorAnimation(Color startColor, Color targetColor, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            buttonImage.color = Color.Lerp(startColor, targetColor, t);
            time += Time.deltaTime;
            yield return null;
        }
        buttonImage.color = targetColor;
    }
    
    private IEnumerator ShadowAnimation(Vector2 startDist, Vector2 targetDist, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            shadowComponent.effectDistance = Vector2.Lerp(startDist, targetDist, t);
            time += Time.deltaTime;
            yield return null;
        }
        shadowComponent.effectDistance = targetDist;
    }
    
    private IEnumerator BreathingAnimation()
    {
        Vector3 baseScale = new Vector3(
            originalScale.x * hoverEffects.hoverScale.x,
            originalScale.y * hoverEffects.hoverScale.y,
            originalScale.z * hoverEffects.hoverScale.z
        );
        
        float time = 0;
        
        while (isHovering && hoverEffects.enableBreathing)
        {
            // 正弦波呼吸效果
            float breathing = Mathf.Sin(time * hoverEffects.breathingSpeed) * hoverEffects.breathingAmount;
            float scaleMultiplier = 1 + breathing;
            
            rectTransform.localScale = new Vector3(
                baseScale.x * scaleMultiplier,
                baseScale.y * scaleMultiplier,
                baseScale.z
            );
            
            time += Time.deltaTime;
            yield return null;
        }
    }
    
    // 公开方法
    public void EnableBreathingEffect(bool enable)
    {
        hoverEffects.enableBreathing = enable;
        
        if (!enable && breathingCoroutine != null)
        {
            StopCoroutine(breathingCoroutine);
            breathingCoroutine = null;
        }
    }
    
    public void SetHoverScale(float scale)
    {
        hoverEffects.hoverScale = new Vector3(scale, scale, 1f);
    }
    
    public void SetBreathingParameters(float amount, float speed)
    {
        hoverEffects.breathingAmount = amount;
        hoverEffects.breathingSpeed = speed;
    }
    
    void OnDisable()
    {
        ResetButton();
    }
    
    public void ResetButton()
    {
        StopAllCoroutines();
        rectTransform.localScale = originalScale;
        
        if (buttonImage != null && hoverEffects.enableColorChange)
        {
            buttonImage.color = hoverEffects.originalColor;
        }
        
        if (shadowComponent != null && hoverEffects.enableShadowEffect)
        {
            shadowComponent.effectDistance = Vector2.zero;
        }
        
        isHovering = false;
        breathingCoroutine = null;
    }
}