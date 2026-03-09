// Assets/Scripts/Weapons/MeleeWhirlWeapon.cs
using UnityEngine;
using SIGame.Enums;
using System.Collections;
using System.Collections.Generic;

public class MeleeWhirlWeapon : WeaponBehavior
{
    [Header("旋风飞刃配置")]
    public float orbitRadius = 2.5f;          // 环绕半径
    public float orbitSpeed = 20f;            // 环绕速度（弧度/秒）
    public float maxRange = 8f;               // 最大攻击距离
    public LayerMask enemyLayer;
    public float flySpeed = 15f;              // 追击速度

    private IDamageable currentTarget = null;
    private Transform currentTargetTransform = null;
    private bool isCurrentTargetAnEnemyController = false;
    private bool isAttacking = false;
    private Coroutine orbitRoutine;
    private float currentOrbitAngle = 0f;
    private float attkMagnification = 3f;

    // 👇 新增：标识是否正在飞向目标（用于朝向控制）
    private bool isFlyingToTarget = false;

    private Vector3 orbitCenter => owner != null ? owner.transform.position : transform.position;

    void Awake()
    {
        orbitRadius += Random.Range(-1f, 1f);
        orbitSpeed += Random.Range(-1f, 1f);
    }

    protected override float GetAttackInterval()
    {
        float attackSpeed = statSystem?.GetFinalValue(PlayerStatAttr.AttackSpeed) ?? 1f;
        return attackSpeed > 0 ? 1f / attackSpeed : 1f;
    }

    protected override void StartAttackRoutine()
    {
        if (orbitRoutine != null) StopCoroutine(orbitRoutine);
        orbitRoutine = StartCoroutine(OrbitAndAttack());
    }

    private IEnumerator OrbitAndAttack()
    {
        if (owner != null)
        {
            Vector3 toSelf = transform.position - orbitCenter;
            currentOrbitAngle = Mathf.Atan2(toSelf.z, toSelf.x);
        }

        while (gameObject != null && owner != null && statSystem != null)
        {
            FindClosestTarget();

            float dynamicInterval = GetAttackInterval();

            if (currentTarget != null &&
                currentTargetTransform != null &&
                Vector3.Distance(orbitCenter, currentTargetTransform.position) <= maxRange &&
                Time.time >= lastAttackTime + dynamicInterval)
            {
                isAttacking = true;
                yield return AttackTarget();
                isAttacking = false;
                lastAttackTime = Time.time;
            }
            else
            {
                // 未攻击：持续环绕 + 刀尖朝外
                currentOrbitAngle += orbitSpeed * Time.deltaTime;
                Vector3 targetPos = orbitCenter + new Vector3(
                    Mathf.Cos(currentOrbitAngle) * orbitRadius,
                    0,
                    Mathf.Sin(currentOrbitAngle) * orbitRadius
                );

                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    Mathf.Clamp01(Time.deltaTime * 5f)
                );

                // ✅ 环绕时：刀尖朝外（径向）
                UpdateFacingDirection();
            }

            yield return null;
        }
    }

    private void FindClosestTarget()
    {
        if (owner == null || !isActiveAndEnabled) return;

        Collider[] colliders = Physics.OverlapSphere(orbitCenter, maxRange, enemyLayer);

        IDamageable bestTarget = null;
        Transform bestTransform = null;
        float closestSqrDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col == null) continue;

            if (col.TryGetComponent(out IDamageable target))
            {
                if (target is EnemyController ec && ec.IsDead) continue;
                if (target is SimpleBossAI boss && boss.IsDead) continue;

                float sqrDist = (col.transform.position - orbitCenter).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    bestTarget = target;
                    bestTransform = col.transform;
                }
            }
        }

        if (bestTarget == currentTarget) return;

        if (isCurrentTargetAnEnemyController && currentTarget is EnemyController oldEnemy)
        {
            EnemyController.ReleaseLock(oldEnemy);
        }

        currentTarget = bestTarget;
        currentTargetTransform = bestTransform;
        isCurrentTargetAnEnemyController = (bestTarget is EnemyController);

        if (isCurrentTargetAnEnemyController)
        {
            var newEnemy = (EnemyController)bestTarget;
            if (!EnemyController.TryLock(newEnemy))
            {
                currentTarget = null;
                currentTargetTransform = null;
                isCurrentTargetAnEnemyController = false;
            }
        }
    }

    private IEnumerator AttackTarget()
    {
        if (currentTarget == null || currentTargetTransform == null || owner == null)
            yield break;

        isFlyingToTarget = true; // 👈 标记进入攻击飞行

        Vector3 startPos = transform.position;
        Vector3 endPos = currentTargetTransform.position;
        float dist = Vector3.Distance(startPos, endPos);
        float duration = Mathf.Max(0.1f, dist / flySpeed);

        float t = 0;
        while (t < 1f && currentTarget != null)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // ✅ 攻击途中：刀尖始终朝向敌人
            LookAtTarget();

            yield return null;
        }

        DealDamage();
        yield return ReturnToOrbit();

        isFlyingToTarget = false; // 👈 攻击结束
    }

    private IEnumerator ReturnToOrbit()
    {
        if (owner == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 targetOrbitPos = orbitCenter + new Vector3(
            Mathf.Cos(currentOrbitAngle) * orbitRadius,
            0,
            Mathf.Sin(currentOrbitAngle) * orbitRadius
        );

        float distance = Vector3.Distance(startPos, targetOrbitPos);
        float duration = Mathf.Max(0.05f, distance / flySpeed);

        float startTime = Time.time;
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            transform.position = Vector3.Lerp(startPos, targetOrbitPos, t);

            // ✅ 返回轨道：刀尖朝外（与环绕一致）
            UpdateFacingDirection();

            yield return null;
        }

        transform.position = targetOrbitPos;
        UpdateFacingDirection(); // 最终对齐
    }

    // ✅ 刀尖朝向外侧（径向）—— 用于环绕和回程
    private void UpdateFacingDirection()
    {
        Vector3 outward = transform.position - orbitCenter;
        outward.y = 0f; // 锁定在水平面
        if (outward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        }
    }

    // ✅ 刀尖朝向当前目标敌人 —— 仅用于攻击飞行阶段
    private void LookAtTarget()
    {
        if (currentTargetTransform == null) return;

        Vector3 direction = currentTargetTransform.position - transform.position;
        direction.y = 0f; // 可选：保持水平攻击（避免上下倾斜）
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    private void DealDamage()
    {
        if (currentTarget == null || statSystem == null) return;

        // 1️⃣ 使用 DamageCalculator 计算原始伤害 + 暴击
        var (rawDamage, isCritical) = DamageCalculator.CalculateMeleeDamageWithCritFlag(statSystem, attkMagnification);

        // 2️⃣ 使用 DamageCalculator 应用目标的护甲和减伤，得到最终伤害
        float finalDamage = DamageCalculator.CalculateFinalDamage(rawDamage, currentTarget);

        // 3️⃣ 获取击退力
        float knockBackForce = DamageCalculator.GetKnockbackForce(statSystem);
        Vector3 knockBackDir = (currentTargetTransform.position - owner.transform.position).normalized;

        // 4️⃣ 构造 DamageInfo 并造成伤害
        DamageInfo damageInfo = new DamageInfo()
        {
            damage = finalDamage,
            knockBackForce = knockBackForce,
            knockBackDirection = knockBackDir,
            source = owner,
            isCritical = isCritical
        };

        // 可选：日志调试
        // Debug.Log($"旋风飞刃造成 {finalDamage:F1} 伤害 (暴击: {isCritical})");

        currentTarget.TakeDamage(damageInfo);
    }

    protected override void PerformAttack() { /* Not used */ }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (isCurrentTargetAnEnemyController && currentTarget is EnemyController ec)
        {
            EnemyController.ReleaseLock(ec);
        }

        if (orbitRoutine != null)
            StopCoroutine(orbitRoutine);
    }

    public bool IsAttacking => isAttacking && currentTarget != null;
}