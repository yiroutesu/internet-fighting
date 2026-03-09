using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFSM : MonoBehaviour
{
    BossContext ctx;
    IBossState curState;
    Dictionary<Type, IBossState> stateCache = new Dictionary<Type, IBossState>();
    
    // 用于Inspector显示的状态信息
    [SerializeField, Header("当前状态")] 
    private string currentStateName = "None";
    
    [SerializeField, Header("状态参数")]
    private string stateDebugInfo = "";
    
    [SerializeField, Header("状态持续时间")]
    private float stateDuration = 0f;
    
    private float stateEnterTime;
    
    public string CurrentStateName => currentStateName;
    public IBossState CurrentState => curState;

    void Awake()
    {
        ctx = GetComponent<BossContext>();
    }

    void Start()
    {
        ChangeState<BossIdleState>();
    }

    void Update()
    {
        curState?.OnUpdate(ctx);
        
        // 更新状态持续时间
        if (curState != null)
        {
            stateDuration = Time.time - stateEnterTime;
        }
    }

    public void ChangeState<T>() where T : IBossState, new()
    {
        if (curState != null) curState.OnExit(ctx);

        var type = typeof(T);
        if (!stateCache.ContainsKey(type))
        {
            var newState = new T();
            if (newState is BossStateBase stateBase)
                stateBase.SetMonoBehaviour(this);
            stateCache[type] = newState;
        }
        curState = stateCache[type];
        
        // 更新显示信息
        currentStateName = type.Name;
        stateEnterTime = Time.time;
        stateDuration = 0f;
        stateDebugInfo = GetStateDebugInfo();

        curState.OnEnter(ctx);
    }
    
    private string GetStateDebugInfo()
    {
        if (ctx == null) return "";
        
        var info = new System.Text.StringBuilder();
        info.AppendLine($"血量: {ctx.hp:F0}/{ctx.maxHp:F0}");
        info.AppendLine($"玩家距离: {ctx.PlayerDist:F1}m");
        info.AppendLine($"狂暴模式: {ctx.IsEnraged}");
        info.AppendLine($"正在攻击: {ctx.isAttacking}");
        info.AppendLine($"正在冲刺: {ctx.isDashing}");
        
        return info.ToString();
    }
    
    // 用于Editor中刷新显示
    void OnValidate()
    {
        if (curState != null)
        {
            currentStateName = curState.GetType().Name;
        }
    }
}