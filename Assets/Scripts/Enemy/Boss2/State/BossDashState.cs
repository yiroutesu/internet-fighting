using System.Collections;
using UnityEngine;

public class BossDashState : BossStateBase
{
    public override void OnEnter(BossContext ctx)
    {
        SetMonoBehaviour(ctx.GetComponent<MonoBehaviour>());
        StartCoroutine(DashAttack(ctx));
    }

    public override void OnUpdate(BossContext ctx)
    {
        // 攻击期间由协程处理
    }
    
    private IEnumerator DashAttack(BossContext ctx)
    {
        if (ctx.player == null || ctx.IsDead) yield break;
        
        ctx.isAttacking = true;
        
        for (int i = 0; i < ctx.dashCount; i++)
        {
            if (ctx.player == null || ctx.IsDead) break;
            
            Vector3 directionToPlayer = ctx.player.position - ctx.transform.position;
            directionToPlayer.y = 0;
            
            if (directionToPlayer.magnitude > 0.1f)
            {
                ctx.dashDirection = directionToPlayer.normalized;
                ctx.transform.rotation = Quaternion.LookRotation(ctx.dashDirection);
            }
            else
            {
                ctx.dashDirection = ctx.transform.forward;
            }
            
            ctx.onDashStart?.Invoke(ctx.dashDirection);
            
            if (ctx.dashVFX != null) ctx.dashVFX.Play();
            if (ctx.audioSrc != null && ctx.dashSFX != null)
                ctx.audioSrc.PlayOneShot(ctx.dashSFX);
            
            ctx.afterimageInterval = ctx.dashDuration / Mathf.Max(1, ctx.DEffectNum);
            ctx.afterimageTimer = 0f;
            
            ctx.isDashing = true;
            ctx.dashTimer = 0f;

            //旋转动画
            ctx.anim.RotateTwoFaceWithDuration(CubeController.Face.Bottom,CubeController.Face.Top, 180, ctx.dashDuration);
            
            // 冲刺过程
            while (ctx.dashTimer < ctx.dashDuration)
            {
                if (ctx.IsDead) break;
                
                ctx.dashTimer += Time.deltaTime;
                Vector3 nextPos = ctx.transform.position + ctx.transform.forward * ctx.CurrentDashSpeed * Time.deltaTime;
                
                if (ctx.playGround != null)
                {
                    Collider groundCol = ctx.playGround.GetComponent<Collider>();
                    if (groundCol != null)
                        nextPos = groundCol.bounds.ClosestPoint(nextPos);
                }
                
                if (ctx.rb != null)
                    ctx.rb.MovePosition(nextPos);
                else
                    ctx.transform.position = nextPos;
                
                // 残影效果
                ctx.afterimageTimer += Time.deltaTime;
                if (ctx.afterimageTimer >= ctx.afterimageInterval && ctx.DashEffectPrefab != null)
                {
                    ctx.afterimageTimer = 0f;
                    GameObject effect = GameObject.Instantiate(ctx.DashEffectPrefab, ctx.transform.position, ctx.transform.rotation);
                    GameObject.Destroy(effect, 1f);
                }
                
                // 伤害检测
                Collider[] hits = Physics.OverlapSphere(ctx.transform.position, 0.8f, ctx.playerLayer);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        Health health = hit.GetComponent<Health>();
                        if (health != null)
                            health.TakeDamage(ctx.dashDamage);
                    }
                }
                
                yield return null;
            }
            
            ctx.isDashing = false;
            
            if (i < ctx.dashCount - 1)
                yield return new WaitForSeconds(ctx.betweenDashDelay);
        }
        
        ctx.lastDashTime = Time.time;
        yield return new WaitForSeconds(0.15f);
        
        if (!ctx.IsDead)
            ctx.GetComponent<BossFSM>().ChangeState<BossTrackState>();
    }
}