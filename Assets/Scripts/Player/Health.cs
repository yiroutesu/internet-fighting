using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using SIGame.Enums;
using System;

public class Health : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("玩家属性配置（用于获取回血、无敌等配置）")]
    [SerializeField] private PlayerStatsSO playerStatsSO;
    
    [Tooltip("属性系统（用于获取最大生命值）")]
    private StatSystem statSystem;

    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }   // 为了让 Unity 能序列化
    public IntEvent OnDamaged;                    // 在 Inspector 里也能拖方法
    public UnityEvent OnDead;

    // 运行时状态（不保存在 SO 中）
    private int currentHealth;
    private float invincibleTimer = -1f; // 初始化时不是无敌状态
    private float regenTimer = 0f;

    void Start()
    {
        statSystem=StatSystem.Instance;
        currentHealth= (int)statSystem.GetFinalValue(PlayerStatAttr.MaxHP);
        StatSystem.Instance.OnStatChanged.AddListener(OnPlayerStatChanged);
    }

    private void OnPlayerStatChanged(PlayerStatAttr attr, float arg1)
    {
        if (attr == PlayerStatAttr.MaxHP)
        {
            currentHealth=(int)arg1;
        }
    }

    void Update()
    {
        // 处理生命恢复
        if (playerStatsSO != null && playerStatsSO.regenPerSec > 0f && regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0f)
            {
                // 开始回血
                int maxHP = statSystem != null 
                    ? Mathf.RoundToInt(statSystem.GetFinalValue(PlayerStatAttr.MaxHP))
                    : Mathf.RoundToInt(playerStatsSO.maxHP);
                
                if (currentHealth < maxHP)
                {
                    currentHealth = Mathf.Min(currentHealth + Mathf.RoundToInt(playerStatsSO.regenPerSec * Time.deltaTime), maxHP);
                }
            }
        }

        if (currentHealth <= 0)
        {
            GameManager.Instance?.onGameOver.Invoke();
        }
    }

    // 外部调用这个接口即可扣血
    public void TakeDamage(int amount)
    {
        if (IsInvincible()) return; // 如果无敌，直接返回（不扣血）

        int maxHP = statSystem != null 
            ? Mathf.RoundToInt(statSystem.GetFinalValue(PlayerStatAttr.MaxHP))
            : (playerStatsSO != null ? Mathf.RoundToInt(playerStatsSO.maxHP) : 100);
        
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHP);
        ScreenShakeManager.Instance?.Shake();
        OnDamaged?.Invoke(currentHealth); // UI 更新
        
        // 触发无敌状态
        if (playerStatsSO != null && playerStatsSO.invincibleDuration > 0f)
        {
            invincibleTimer = playerStatsSO.invincibleDuration;
            StartCoroutine(StartInvincibleCounting());
        }
        
        // 重置回血计时器
        if (playerStatsSO != null)
        {
            regenTimer = playerStatsSO.regenDelay;
        }

        if (currentHealth <= 0)
            OnDead?.Invoke();
    }
    
    /// <summary>
    /// 检查是否处于无敌状态
    /// </summary>
    public bool IsInvincible()
    {
        return invincibleTimer > 0f;
    }
    
    IEnumerator StartInvincibleCounting()
    {
        // 等待无敌时间结束
        while (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
            yield return null; // 每帧暂停一次
        }

        // 无敌时间结束后，重置无敌状态
        invincibleTimer = -1f; // 确保无敌状态结束
    }
    
    /// <summary>
    /// 获取当前生命值
    /// </summary>
    public int GetCurrentHealth() => currentHealth;
    
    /// <summary>
    /// 获取最大生命值（从 StatSystem 获取）
    /// </summary>
    public int GetMaxHealth()
    {
        if (statSystem != null)
            return Mathf.RoundToInt(statSystem.GetFinalValue(PlayerStatAttr.MaxHP));
        if (playerStatsSO != null)
            return Mathf.RoundToInt(playerStatsSO.maxHP);
        return 100;
    }
}