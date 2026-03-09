using System;
using System.Collections;
using SIGame.Enums;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarSmooth : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    VisualElement fill  => uiDocument.rootVisualElement.Q<VisualElement>("Fill");
    Label        txt    => uiDocument.rootVisualElement.Q<Label>("ValueText");

    [Header("参数")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float smoothSpeed = 3.5f; // 越大越快

    [Header("颜色")]
[SerializeField] private Color fullHpColor  = new Color(0.22f, 0.77f, 0.34f); //绿
[SerializeField] private Color lowHpColor   = new Color(0.85f, 0.16f, 0.13f); //红

    private ProgressBar bar;
    private float currentHp;          // 当前真实血量（插值过程中会渐变）
    private float targetHp;           // 目标血量
    private Coroutine hpAnim;
    
    [SerializeField]private Health health;

    private void Start() {
        health.OnDamaged.AddListener(OnHpChanged); 
    }
    /* 第一次赋值 */
    private void Awake()
    {
        bar = uiDocument.rootVisualElement.Q<ProgressBar>("HealthBar");
        currentHp = maxHp;
        targetHp = maxHp;
        RefreshUI();                  // 初始 100%
        StatSystem.Instance.OnStatChanged.AddListener(OnPlayerStatChanged);
    }

    private void OnPlayerStatChanged(PlayerStatAttr attr, float finalValue)
    {
        if (attr == PlayerStatAttr.MaxHP)
        {
            OnMaxHpChanged();
            RefreshUI();
        }
    }

    private void OnHpChanged(int newHp)
    {
        // newHp 是 Health 传过来的当前血量
        float delta = newHp - targetHp;   // 算出变化量
        ChangeHp(delta);                  // 复用原来的平滑动画
    }
    private void ChangeHp(float amount)
    {
        targetHp = Mathf.Clamp(targetHp + amount, 0, maxHp);
        if (hpAnim != null) StopCoroutine(hpAnim);
        hpAnim = StartCoroutine(SmoothHp());
    }

    /* 协程：把 currentHp 平滑到 targetHp */
private IEnumerator SmoothHp()
{
    float start = currentHp;
    float t = 0;                       // 0~1 的进度
    float dur = 1f / smoothSpeed;      // 把速度系数转成时长，方便调手感

    while (t < 1)
    {
        t += Time.deltaTime / dur;
        float eased = 1f - Mathf.Pow(1f - t, 3);   // EaseOutCubic
        currentHp = Mathf.Lerp(start, targetHp, eased);
        RefreshUI();
        yield return null;
    }
    currentHp = targetHp;
    RefreshUI();
}

    /* 刷新 ProgressBar 与颜色 */
    private void RefreshUI()
    {
        float percent = currentHp / maxHp;
        bar.value = percent * 100f;

        // 血量数值更新
        bar.title = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        float pct = currentHp / maxHp;
        fill.style.width = Length.Percent(pct * 100f);   // 宽度插值


        // 颜色三段旧版
        //bar.EnableInClassList("lowHealth",    percent < 0.3f);
        //bar.EnableInClassList("mediumHealth", percent >= 0.3f && percent < 0.6f);
        //bar.EnableInClassList("highHealth",   percent >= 0.6f);
        bar.value = percent * 100f;
        bar.title = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";

        // 颜色插值
        Color nowColor = Color.Lerp(lowHpColor, fullHpColor, percent);
        fill.style.backgroundColor = Color.Lerp(lowHpColor, fullHpColor, pct);
        txt.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        SetBarColor(nowColor);
    }
    private void SetBarColor(Color c)
    {
        // 把 0~1 映射到 0~255 并写进 USS 变量
        bar.style.backgroundColor = new Color(c.r, c.g, c.b, 1);
        fill.style.backgroundColor = new Color(c.r, c.g, c.b, 1);
    }

    /* 键盘测试：A 扣 10 滴，D 加 10 滴 */
    private void Update()
    {
        //测试用
        //if (Input.GetKeyDown(KeyCode.LeftArrow)) ChangeHp(-10);
        //if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeHp(+10);
    }
        void OnDestroy()
    {
        health.OnDamaged.RemoveListener(OnHpChanged);   // ④ 把自己从列表移除
    }
        private void OnMaxHpChanged()
    {
        maxHp=StatSystem.Instance.GetFinalValue(SIGame.Enums.PlayerStatAttr.MaxHP);
        currentHp=maxHp;
        Debug.Log("maxhp="+maxHp);
        RefreshUI();
    }
}