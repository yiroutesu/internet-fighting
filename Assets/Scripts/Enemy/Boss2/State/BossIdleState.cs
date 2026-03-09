using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : BossStateBase
{
    float timer;
    
    public override void OnEnter(BossContext ctx)
    {
        timer = Random.Range(1f, 2f);
        ctx.isAttacking = false;
    }

    public override void OnUpdate(BossContext ctx)
    {
        if (ctx.player == null || ctx.IsDead) return;
        
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ctx.GetComponent<BossFSM>().ChangeState<BossTrackState>();
        }

        // 玩家进入攻击范围直接攻击
        if (ctx.PlayerDist <= ctx.meleeRange && ctx.CanMelee)
        {
            ctx.GetComponent<BossFSM>().ChangeState<BossMeleeState>();
        }
        else if (ctx.PlayerDist <= ctx.dashTriggerRange && ctx.CanDash)
        {
            ctx.GetComponent<BossFSM>().ChangeState<BossDashState>();
        }
    }
}