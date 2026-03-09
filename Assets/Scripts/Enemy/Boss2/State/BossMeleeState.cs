using System.Collections;
using UnityEngine;

public class BossMeleeState : BossStateBase
{
    public override void OnEnter(BossContext ctx)
    {
        SetMonoBehaviour(ctx.GetComponent<MonoBehaviour>());
        StartCoroutine(MeleeAttack(ctx));
    }

    public override void OnUpdate(BossContext ctx)
    {
        // 攻击期间不做其他事情
    }
    
    private IEnumerator MeleeAttack(BossContext ctx)
    {
        if (ctx.IsDead) yield break;
        
        ctx.isAttacking = true;
        ctx.lastMeleeTime = Time.time;
        ctx.onMeleeStart?.Invoke();
        
        // 转向玩家
        if (ctx.player != null)
        {
            Vector3 flatDir = ctx.player.position - ctx.transform.position;
            flatDir.y = 0;
            if (flatDir.magnitude > 0.1f)
            {
                ctx.transform.rotation = Quaternion.LookRotation(flatDir.normalized);
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 执行攻击
        if (!ctx.IsDead)
        {
            Collider[] hits = Physics.OverlapSphere(ctx.transform.position, ctx.meleeRadius, ctx.playerLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    Health health = hit.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(ctx.meleeDamage);
                        Debug.Log($"Boss 近战命中 {hit.name}");
                    }
                }
            }
        }
        
        yield return new WaitForSeconds(0.7f);
        
        // 返回追踪状态
        if (!ctx.IsDead)
            ctx.GetComponent<BossFSM>().ChangeState<BossTrackState>();
    }
}