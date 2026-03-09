using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StateTransitionRule
{
    public System.Type fromState;           // 从哪个状态切换
    public System.Type toState;             // 切换到哪个状态
    public float baseWeight = 1.0f;         // 基础权重
    public float distanceWeight = 1.0f;     // 距离权重因子
    public float minDistance = 0f;          // 最小有效距离
    public float maxDistance = float.MaxValue; // 最大有效距离
    public float optimalDistance = 5f;      // 最佳距离（权重最高）
    public float weightFalloff = 1.0f;      // 距离衰减系数
    
    public bool requireCooldown = false;    // 是否需要冷却完成
    public bool requireEnraged = false;     // 是否需要狂暴状态
    public bool requireNormal = false;      // 是否需要非狂暴状态
}

public class BossStateMachineController : MonoBehaviour
{
    [Header("状态机设置")]
    public BossFSM fsm;
    public BossContext ctx;
    
    [Header("状态切换规则")]
    public List<StateTransitionRule> transitionRules = new List<StateTransitionRule>();
    
    [Header("调试信息")]
    public System.Type lastState;
    public System.Type currentState;
    public Dictionary<System.Type, float> stateWeights = new Dictionary<System.Type, float>();
    
    private Dictionary<System.Type, StateTransitionRule[]> ruleCache = new Dictionary<System.Type, StateTransitionRule[]>();
    
    void Awake()
    {
        if (fsm == null)
            fsm = GetComponent<BossFSM>();
        if (ctx == null)
            ctx = GetComponent<BossContext>();
            
        // 缓存规则以提高性能
        CacheRules();
    }
    
    void Start()
    {
        if (fsm != null)
        {
            currentState = fsm.CurrentState?.GetType();
            lastState = null;
        }
    }
    
    void Update()
    {
        if (fsm == null || ctx == null) return;
        
        // 更新当前状态
        currentState = fsm.CurrentState?.GetType();
    }
    
    /// <summary>
    /// 根据当前状态和条件计算下一个状态
    /// </summary>
    public System.Type EvaluateNextState()
    {
        if (fsm == null || ctx == null || currentState == null)
            return null;
            
        // 如果玩家不存在或BOSS死亡，返回空
        if (ctx.player == null || ctx.IsDead)
            return null;
            
        // 如果正在攻击中，不切换状态
        if (ctx.isAttacking || ctx.isDashing)
            return null;
            
        // 获取从当前状态出发的所有规则
        if (!ruleCache.TryGetValue(currentState, out StateTransitionRule[] rules))
            return null;
            
        // 计算每个状态的权重
        stateWeights.Clear();
        
        foreach (var rule in rules)
        {
            // 检查规则条件
            if (!CheckRuleConditions(rule))
                continue;
                
            // 计算权重
            float weight = CalculateStateWeight(rule);
            
            // 累加相同目标状态的权重
            if (stateWeights.ContainsKey(rule.toState))
                stateWeights[rule.toState] += weight;
            else
                stateWeights[rule.toState] = weight;
        }
        
        // 如果没有可用的状态，返回null
        if (stateWeights.Count == 0)
            return null;
            
        // 选择权重最高的状态
        System.Type bestState = null;
        float highestWeight = float.MinValue;
        
        foreach (var kvp in stateWeights)
        {
            // 跳过当前状态
            if (kvp.Key == currentState)
                continue;
                
            // 检查目标状态是否可用（基于冷却等）
            if (!IsStateAvailable(kvp.Key))
                continue;
                
            if (kvp.Value > highestWeight)
            {
                highestWeight = kvp.Value;
                bestState = kvp.Key;
            }
        }
        
        return bestState;
    }
    
    /// <summary>
    /// 切换到下一个状态（基于权重计算）
    /// </summary>
    public void TransitionToWeightedState()
    {
        System.Type nextState = EvaluateNextState();
        
        if (nextState != null && nextState != currentState)
        {
            // 记录上一个状态
            lastState = currentState;
            
            // 切换到新状态
            ChangeState(nextState);
        }
    }
    
    /// <summary>
    /// 立即切换到指定状态
    /// </summary>
    public void ChangeState(System.Type stateType)
    {
        if (fsm == null) return;
        
        // 记录上一个状态
        lastState = currentState;
        
        // 使用反射调用ChangeState<T>
        var method = typeof(BossFSM).GetMethod("ChangeState");
        var genericMethod = method.MakeGenericMethod(stateType);
        genericMethod.Invoke(fsm, null);
    }
    
    /// <summary>
    /// 检查规则条件
    /// </summary>
    private bool CheckRuleConditions(StateTransitionRule rule)
    {
        // 检查距离范围
        float distance = ctx.PlayerDist;
        if (distance < rule.minDistance || distance > rule.maxDistance)
            return false;
            
        // 检查狂暴状态要求
        if (rule.requireEnraged && !ctx.IsEnraged)
            return false;
            
        if (rule.requireNormal && ctx.IsEnraged)
            return false;
            
        // 检查冷却要求
        if (rule.requireCooldown && !IsStateAvailable(rule.toState))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// 计算状态权重
    /// </summary>
    private float CalculateStateWeight(StateTransitionRule rule)
    {
        float distance = ctx.PlayerDist;
        
        // 基础权重
        float weight = rule.baseWeight;
        
        // 距离权重（使用高斯衰减函数）
        float distanceFactor = Mathf.Exp(-Mathf.Pow(distance - rule.optimalDistance, 2) / 
                                        (2 * rule.weightFalloff * rule.weightFalloff));
        weight *= rule.distanceWeight * distanceFactor;
        
        // 上一个状态惩罚（避免重复切换）
        if (lastState == rule.toState)
            weight *= 0.5f;
            
        return weight;
    }
    
    /// <summary>
    /// 检查状态是否可用（基于冷却等）
    /// </summary>
    private bool IsStateAvailable(System.Type stateType)
    {
        // 根据状态类型检查冷却
        if (stateType == typeof(BossMeleeState))
            return ctx.CanMelee;
        else if (stateType == typeof(BossDashState))
            return ctx.CanDash;
        else if (stateType == typeof(BossLaserState) || stateType == typeof(BossSweepLaserState))
            return ctx.CanLaser;
            
        return true;
    }
    
    /// <summary>
    /// 缓存规则以提高性能
    /// </summary>
    private void CacheRules()
    {
        ruleCache.Clear();
        
        foreach (var rule in transitionRules)
        {
            if (!ruleCache.ContainsKey(rule.fromState))
                ruleCache[rule.fromState] = new StateTransitionRule[0];
                
            var list = new List<StateTransitionRule>(ruleCache[rule.fromState]);
            list.Add(rule);
            ruleCache[rule.fromState] = list.ToArray();
        }
    }
    
    /// <summary>
    /// 初始化默认规则（可在Inspector中调整）
    /// </summary>
    public void InitializeDefaultRules()
    {
        transitionRules.Clear();
        
        // 从Idle状态切换的规则
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossIdleState),
            toState = typeof(BossTrackState),
            baseWeight = 1.0f,
            distanceWeight = 1.0f,
            minDistance = 0f,
            maxDistance = float.MaxValue,
            optimalDistance = 5f
        });
        
        // 从Track状态切换的规则
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossTrackState),
            toState = typeof(BossMeleeState),
            baseWeight = 1.5f,
            distanceWeight = 2.0f,
            minDistance = 0f,
            maxDistance = 3f,
            optimalDistance = 1.5f,
            requireCooldown = true
        });
        
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossTrackState),
            toState = typeof(BossDashState),
            baseWeight = 1.2f,
            distanceWeight = 1.5f,
            minDistance = 0f,
            maxDistance = 4f,
            optimalDistance = 2.5f,
            requireCooldown = true
        });
        
        // 激光攻击规则（根据血量选择）
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossTrackState),
            toState = typeof(BossSweepLaserState),
            baseWeight = 1.0f,
            distanceWeight = 1.8f,
            minDistance = 3f,
            maxDistance = 12f,
            optimalDistance = 8f,
            requireCooldown = true,
            requireNormal = true  // 非狂暴时使用扫射激光
        });
        
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossTrackState),
            toState = typeof(BossLaserState),
            baseWeight = 1.2f,
            distanceWeight = 1.8f,
            minDistance = 3f,
            maxDistance = 12f,
            optimalDistance = 8f,
            requireCooldown = true,
            requireEnraged = true  // 狂暴时使用普通激光
        });
        
        // 从攻击状态返回追踪的规则
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossMeleeState),
            toState = typeof(BossTrackState),
            baseWeight = 2.0f,
            distanceWeight = 0.5f
        });
        
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossDashState),
            toState = typeof(BossTrackState),
            baseWeight = 2.0f,
            distanceWeight = 0.5f
        });
        
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossSweepLaserState),
            toState = typeof(BossTrackState),
            baseWeight = 2.0f,
            distanceWeight = 0.5f
        });
        
        transitionRules.Add(new StateTransitionRule
        {
            fromState = typeof(BossLaserState),
            toState = typeof(BossTrackState),
            baseWeight = 2.0f,
            distanceWeight = 0.5f
        });
        
        CacheRules();
    }
    
    /// <summary>
    /// 获取状态权重信息（用于调试）
    /// </summary>
    public string GetWeightsDebugInfo()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("当前状态权重:");
        
        foreach (var kvp in stateWeights)
        {
            sb.AppendLine($"  {kvp.Key.Name}: {kvp.Value:F2}");
        }
        
        sb.AppendLine($"上一个状态: {(lastState != null ? lastState.Name : "None")}");
        sb.AppendLine($"建议下一个状态: {EvaluateNextState()?.Name ?? "None"}");
        
        return sb.ToString();
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 在Scene视图中显示状态切换信息
        if (Application.isPlaying && fsm != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                $"状态: {currentState?.Name ?? "None"}\n" +
                $"上一个: {lastState?.Name ?? "None"}\n" +
                $"距离: {ctx?.PlayerDist:F1}m",
                new GUIStyle { normal = { textColor = Color.yellow }, fontSize = 9 }
            );
        }
    }
#endif
}