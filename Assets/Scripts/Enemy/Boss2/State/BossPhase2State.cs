using UnityEngine;

public class BossPhase2State : BossStateBase
{
    public override void OnEnter(BossContext ctx)
    {
        Debug.Log("Boss 进入第二阶段！");
        // 可以在这里添加第二阶段特有的效果，比如变身、特殊音效等
        ctx.GetComponent<BossFSM>().ChangeState<BossTrackState>();
    }

    public override void OnUpdate(BossContext ctx)
    {
        // 第二阶段会持续影响行为，由其他状态检查 IsEnraged 属性
    }
}