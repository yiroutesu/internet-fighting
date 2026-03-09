// BossStateBase.cs
using System.Collections;
using UnityEngine;

public abstract class BossStateBase : IBossState
{
    protected MonoBehaviour monoBehaviour;
    protected Coroutine currentCoroutine;
    
    public void SetMonoBehaviour(MonoBehaviour mb)
    {
        monoBehaviour = mb;
    }
    
    protected void StartCoroutine(IEnumerator routine)
    {
        if (monoBehaviour != null)
        {
            StopCurrentCoroutine();
            currentCoroutine = monoBehaviour.StartCoroutine(routine);
        }
    }
    
    protected void StopCurrentCoroutine()
    {
        if (currentCoroutine != null && monoBehaviour != null)
        {
            monoBehaviour.StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }
    
    public virtual void OnExit(BossContext ctx)
    {
        StopCurrentCoroutine();
        ctx.isAttacking = false;
        ctx.isDashing = false;
    }
    
    public abstract void OnEnter(BossContext ctx);
    public abstract void OnUpdate(BossContext ctx);
}