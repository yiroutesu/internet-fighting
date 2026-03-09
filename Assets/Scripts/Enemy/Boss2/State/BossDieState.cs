using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : IBossState
{
    public void OnEnter(BossContext ctx)
    {
        //ctx.anim.Play("Die");
        // 掉宝、UI、销毁
        Debug.Log("die");
        ctx.Die();
    }

    public void OnUpdate(BossContext ctx) { }
    public void OnExit(BossContext ctx)   { }
}
