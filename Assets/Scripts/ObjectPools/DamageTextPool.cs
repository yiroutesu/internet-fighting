// DamageTextPool.cs
// 专为类幸存者设计的伤害数字对象池（修复 inactive 协程问题）
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance;

    [Header("配置")]
    public GameObject damageTextPrefab; // 拖入你的 TMP UI 预制体（必须是 TextMeshPro - Text (UI)）
    public int poolSize = 80;           // 初始池大小（根据峰值伤害数调整）
    public Transform canvas;            // 拖入 Canvas（Screen Space - Overlay）

    private List<DamageTextItem> pool = new List<DamageTextItem>();
    private Queue<DamageTextItem> available = new Queue<DamageTextItem>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    void InitializePool()
    {
        if (damageTextPrefab == null || canvas == null)
        {
            Debug.LogError("DamageTextPool: Prefab 或 Canvas 未赋值！");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(damageTextPrefab, canvas);
            go.SetActive(false); // ✅ 在这里设为 inactive（安全！）
            var item = go.AddComponent<DamageTextItem>();
            pool.Add(item);
            available.Enqueue(item);
        }
    }

    public void Show(Vector3 worldPosition, float damage, bool isCritical)
    {
        if (available.Count == 0)
        {
            // 池满时动态扩容（类幸存者建议保留，避免丢失反馈）
            GameObject go = Instantiate(damageTextPrefab, canvas);
            go.SetActive(false);
            var newItem = go.AddComponent<DamageTextItem>();
            pool.Add(newItem);
            available.Enqueue(newItem);
        }

        var item = available.Dequeue();
        item.Show(worldPosition, damage, isCritical, ReturnToPool);
    }

    void ReturnToPool(DamageTextItem item)
    {
        available.Enqueue(item);
    }
}

// 单个伤害数字组件
public class DamageTextItem : MonoBehaviour
{
    private TMP_Text text;
    private RectTransform rectTransform;
    private System.Action<DamageTextItem> onFadeComplete;

    // 样式参数（可后续暴露到 Inspector）
    private static readonly Color normalColor = new Color(1f, 0.2f, 0.2f);   // 红
    private static readonly Color critColor = new Color(1f, 0.8f, 0.2f);     // 黄
    private const float minFontSize = 80f;
    private const float maxFontSize = 100f;
    private const int maxDamageForScaling = 300;

    void Awake()
    {
        // ✅ 不再调用 SetActive(false)！
        text = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
        
        if (text == null)
        {
            Debug.LogError("DamageTextItem: 缺少 TMP_Text 组件！");
        }
    }

    public void Show(Vector3 worldPos, float damage, bool isCrit, System.Action<DamageTextItem> onComplete)
    {
        gameObject.SetActive(true); // ✅ 激活对象（此时可安全启动协程）
        onFadeComplete = onComplete;

        // 文本内容
        text.text =  Mathf.RoundToInt(damage).ToString()+"!";

        // 字体大小（基于伤害值缩放）
        float fs = Mathf.Lerp(minFontSize, maxFontSize,
            Mathf.InverseLerp(1, maxDamageForScaling, damage));
        text.fontSize = Mathf.Clamp(fs, minFontSize, maxFontSize);

        // 样式
        if (isCrit)
        {
            text.color = critColor;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }
        else
        {
            text.color = normalColor;
            text.fontStyle = FontStyles.Normal;
            text.outlineWidth = 0f;
        }

        // 定位到屏幕位置
        if (Camera.main != null)
        {
            rectTransform.position = Camera.main.WorldToScreenPoint(worldPos);
        }
        else
        {
            Debug.LogWarning("DamageTextItem: 主相机未设置 'MainCamera' 标签！");
            rectTransform.position = worldPos; // fallback（可能错位）
        }

        // 启动淡出动画（现在安全！）
        StartCoroutine(FadeOut(text.fontSize));
    }

    System.Collections.IEnumerator FadeOut(float fontSize)
    {
        float duration = 0.6f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * (fontSize * 0.5f);
        Color startColor = text.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        gameObject.SetActive(false); // 隐藏
        onFadeComplete?.Invoke(this); // 归还到池
    }
}