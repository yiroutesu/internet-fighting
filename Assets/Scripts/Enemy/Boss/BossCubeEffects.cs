using UnityEngine;
using System.Collections;

public class BossCubeEffects : MonoBehaviour
{
    [Header("材质引用")]
    [SerializeField] private Renderer cubeRenderer;
    [SerializeField] private string colorProperty = "_EmissionColor";
    
    [Header("闪烁效果")]
    [SerializeField] private float flashIntensity = 5f;
    [SerializeField] private float flashDuration = 0.2f;
    
    [Header("脉冲效果")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmplitude = 0.1f;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    
    [Header("拖尾效果")]
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private GameObject trailPrefab;
    [SerializeField] private float trailDuration = 0.5f;
    
    private Material cubeMaterial;
    private Color originalColor;
    private Coroutine flashRoutine;
    
    void Start()
    {
        if (cubeRenderer == null)
            cubeRenderer = GetComponent<Renderer>();
        
        if (cubeRenderer != null)
        {
            cubeMaterial = cubeRenderer.material;
            originalColor = cubeMaterial.GetColor(colorProperty);
        }
        
        baseScale = transform.localScale;
    }
    
    void Update()
    {
        if (enablePulse)
        {
            // 心跳脉冲效果
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude + 1f;
            transform.localScale = baseScale * pulse;
        }
    }
    
    public void FlashColor(Color flashColor)
    {
        if (cubeMaterial == null) return;
        
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        
        flashRoutine = StartCoroutine(FlashRoutine(flashColor));
    }
    
    private IEnumerator FlashRoutine(Color flashColor)
    {
        float elapsed = 0f;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            
            // 使用正弦曲线实现闪烁
            float intensity = Mathf.Sin(t * Mathf.PI) * flashIntensity;
            Color currentColor = Color.Lerp(originalColor, flashColor, intensity);
            
            cubeMaterial.SetColor(colorProperty, currentColor);
            yield return null;
        }
        
        // 恢复原色
        cubeMaterial.SetColor(colorProperty, originalColor);
        flashRoutine = null;
    }
    
    public void SpawnTrail(Vector3 direction)
    {
        if (!enableTrail || trailPrefab == null) return;
        
        GameObject trail = Instantiate(trailPrefab, transform.position, transform.rotation);
        Destroy(trail, trailDuration);
    }
    
    public void ChangeMaterial(Material newMaterial)
    {
        if (cubeRenderer == null) return;
        
        cubeRenderer.material = newMaterial;
        cubeMaterial = cubeRenderer.material;
        originalColor = cubeMaterial.GetColor(colorProperty);
    }
    
    void OnDestroy()
    {
        if (cubeMaterial != null && Application.isPlaying)
        {
            Destroy(cubeMaterial);
        }
    }
}