using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossStateManager : MonoBehaviour
{
    public enum BossAttackState { Idle, Dash, Melee, Laser, SweepLaser }
    
    [Header("状态同步")]
    public BossAttackState currentAttackState = BossAttackState.Idle;
    public UnityEvent<BossAttackState> onAttackStateChanged;
    
    [Header("攻击持续时间")]
    public float dashStateDuration = 1f;
    public float meleeStateDuration = 0.5f;
    public float laserStateDuration = 1.2f;
    public float sweepStateDuration = 2f;
    
    private SimpleBossAI bossAI;
    private Coroutine stateRoutine;
    
    void Start()
    {
        bossAI = GetComponent<SimpleBossAI>();
        
        if (bossAI != null)
        {
            // 订阅事件并同步状态
            bossAI.onDashStart.AddListener((dir) => SetAttackState(BossAttackState.Dash, dashStateDuration));
            bossAI.onMeleeStart.AddListener(() => SetAttackState(BossAttackState.Melee, meleeStateDuration));
            bossAI.onLaserStart.AddListener((dir) => SetAttackState(BossAttackState.Laser, laserStateDuration));
            bossAI.onSweepLaserStart.AddListener((dir) => SetAttackState(BossAttackState.SweepLaser, sweepStateDuration));
        }
    }
    
    public void SetAttackState(BossAttackState newState, float duration = 0f)
    {
        currentAttackState = newState;
        onAttackStateChanged?.Invoke(newState);
        
        if (stateRoutine != null)
            StopCoroutine(stateRoutine);
        
        if (duration > 0f)
        {
            stateRoutine = StartCoroutine(StateDurationRoutine(duration));
        }
    }
    
    private IEnumerator StateDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetAttackState(BossAttackState.Idle);
    }
}