using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossTrackState : BossStateBase
{
    private Coroutine movementCoroutine;
    private Coroutine rotationAttackCoroutine;
    private float lastRotationTime = 0f;
    private bool isCubeRotating = false; // 新增：跟踪魔方旋转状态

    public override void OnEnter(BossContext ctx)
    {
        SetMonoBehaviour(ctx.GetComponent<MonoBehaviour>());

        // 重置旋转状态
        isCubeRotating = false;

        // 启动移动协程
        if (movementCoroutine != null)
            ctx.StopCoroutine(movementCoroutine);

        movementCoroutine = ctx.StartCoroutine(MoveTowardsPlayerCoroutine(ctx));

        // 启动魔方旋转攻击协程
        if (rotationAttackCoroutine != null)
            ctx.StopCoroutine(rotationAttackCoroutine);

        rotationAttackCoroutine = ctx.StartCoroutine(RotationAttackCoroutine(ctx));

        lastRotationTime = Time.time;
    }

    public override void OnExit(BossContext ctx)
    {
        // 停止所有协程
        if (movementCoroutine != null)
            ctx.StopCoroutine(movementCoroutine);

        if (rotationAttackCoroutine != null)
            ctx.StopCoroutine(rotationAttackCoroutine);
    }

    public override void OnUpdate(BossContext ctx)
    {
        // 如果魔方正在旋转，禁止状态切换
        if (isCubeRotating) return;
        
        if (ctx.player == null || ctx.IsDead || ctx.isAttacking) return;
        
        float dist = ctx.PlayerDist;
        
        // 根据距离选择攻击
        if (dist <= ctx.dashTriggerRange && ctx.CanDash)
        {
            ctx.GetComponent<BossFSM>().ChangeState<BossDashState>();
        }
        else if (dist <= ctx.meleeRange && ctx.CanMelee)
        {
            ctx.GetComponent<BossFSM>().ChangeState<BossMeleeState>();
        }
        else if (dist > ctx.meleeRange && dist <= ctx.laserRange && ctx.CanLaser)
        {
            bool shouldUseSweep = ctx.useSweepAttack && (ctx.hp / ctx.maxHp > 0.5f);
            if (!ctx.IsEnraged && shouldUseSweep)
            {
                ctx.GetComponent<BossFSM>().ChangeState<BossSweepLaserState>();
            }
            else
            {
                ctx.GetComponent<BossFSM>().ChangeState<BossSweepLaserState>();
            }
        }
        else if (dist > ctx.maxChaseRange)
        {
            // 超出追踪范围，回到空闲状态
            ctx.GetComponent<BossFSM>().ChangeState<BossIdleState>();
        }
    }

    // 移动协程
    private IEnumerator MoveTowardsPlayerCoroutine(BossContext ctx)
    {
        while (!ctx.IsDead && !ctx.isAttacking)
        {
            if (ctx.player == null) yield break;

            float dist = ctx.PlayerDist;

            // 只在追踪范围内移动
            if (dist <= ctx.maxChaseRange && dist > ctx.meleeRange)
            {
                Vector3 flatDir = ctx.player.position - ctx.transform.position;
                flatDir.y = 0;

                if (flatDir.magnitude > 0.1f)
                {
                    flatDir.Normalize();
                    Quaternion targetRot = Quaternion.LookRotation(flatDir);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRot,
                        ctx.rotationSpeed * Time.deltaTime);
                }

                Vector3 moveDir = ctx.transform.forward;
                Vector3 nextPos = ctx.transform.position + moveDir * ctx.CurrentMoveSpeed * Time.deltaTime;

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
            }

            yield return null;
        }
    }

    // 魔方旋转攻击协程
    private IEnumerator RotationAttackCoroutine(BossContext ctx)
    {
        while (!ctx.IsDead && !ctx.isAttacking)
        {
            // 检查是否达到旋转间隔
            if (Time.time - lastRotationTime >= ctx.cubeRotationInterval)
            {
                // 只在追踪状态下执行攻击
                if (ctx.player != null && !ctx.IsDead && !ctx.isAttacking)
                {
                    float dist = ctx.PlayerDist;
                    
                    // 只在适当距离内攻击
                    if (dist <= ctx.maxChaseRange && dist > ctx.meleeRange)
                    {
                        // 标记魔方开始旋转
                        isCubeRotating = true;
                        
                        // 随机选择一个魔方面进行旋转
                        CubeController.Face randomFace = GetRandomFace();
                        
                        // 执行魔方旋转（随机方向：顺时针或逆时针）
                        float angle = 90f; // 旋转90度
                        float speed = 300f; // 旋转速度
                        
                        // 随机选择旋转方向
                        CubeController.RotationDirection direction = Random.Range(0, 2) == 0 ? 
                            CubeController.RotationDirection.Clockwise : 
                            CubeController.RotationDirection.CounterClockwise;
                        
                        // 执行旋转并等待完成
                        yield return ctx.StartCoroutine(PerformRotationAndWait(ctx, randomFace, direction, angle, speed));
                        
                        // 标记魔方旋转完成
                        isCubeRotating = false;
                        
                        // 旋转的同时发射子弹
                        ctx.ShootBullets();
                        
                        // 更新上次旋转时间
                        lastRotationTime = Time.time;
                    }
                }
            }

            yield return null; // 每帧检查
        }
    }
    
    // 执行魔方旋转并等待完成的协程
    private IEnumerator PerformRotationAndWait(BossContext ctx, CubeController.Face face, 
                                              CubeController.RotationDirection direction, 
                                              float angle, float speed)
    {
        bool rotationComplete = false;
        
        // 创建一个回调来标记旋转完成
        System.Action onRotationComplete = () => 
        {
            rotationComplete = true;
        };
        
        // 监听旋转完成事件
        ctx.anim.RotateFaceOver.AddListener(() => onRotationComplete?.Invoke());
        
        // 执行旋转
        ctx.anim.RotateFace(face, direction, angle, speed);
        
        // 等待旋转完成
        while (!rotationComplete)
        {
            yield return null;
        }
        
        // 移除事件监听
        ctx.anim.RotateFaceOver.RemoveListener(() => onRotationComplete?.Invoke());
    }

    // 获取随机魔方面
    private CubeController.Face GetRandomFace()
    {
        // 从所有面中随机选择一个
        CubeController.Face[] allFaces = {
            CubeController.Face.Top,
            CubeController.Face.Bottom,
            CubeController.Face.Left,
            CubeController.Face.Right,
            CubeController.Face.Front,
            CubeController.Face.Back
        };

        return allFaces[Random.Range(0, allFaces.Length)];
    }
}