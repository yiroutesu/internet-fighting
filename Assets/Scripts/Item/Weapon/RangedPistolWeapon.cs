using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGame.Enums;

public class RangedPistolWeapon : WeaponBehavior
{
    [Header("手枪配置")]
    public string bulletTypeKey = "Pistol";      // 对应 BulletPool 中的 key
    public Transform muzzlePoint;                // 枪口发射点
    public float maxRange = 15f;
    public LayerMask enemyLayer;
    public Vector3 hoverOffset = new Vector3(1.5f, 10f, 0f);

    public float targetRefreshInterval = 0.2f; // 每 0.2 秒刷新一次目标（5Hz）
    private float attkMagnification=1f;
    // ✅ 改为 IDamageable，支持 Boss 和普通敌人
    private IDamageable currentTarget = null;
    private float lastTargetUpdateTime = 0f;

    // 用于避免每帧 GC 分配
    private static Collider[] EnemyOverlapResults = new Collider[50];

    void Start()
    {
        int RamNum = UnityEngine.Random.Range(-1, 1);
        hoverOffset.x+=RamNum;
    }
    void LateUpdate()
    {
        if (owner == null) return;

        transform.position = owner.transform.position + owner.transform.TransformVector(hoverOffset);

        // 定期更新目标（不是每帧！）
        if (Time.time - lastTargetUpdateTime > targetRefreshInterval)
        {
            FindClosestEnemy();
            lastTargetUpdateTime = Time.time;
        }

        // 如果有有效目标，平滑瞄准
        if (currentTarget != null && !currentTarget.IsDead)
        {
            Transform targetTransform = GetTransformFromIDamageable(currentTarget);
            if (targetTransform != null)
            {
                AimAtTarget(targetTransform.position);
            }
            else
            {
                currentTarget = null; // 目标无效，清除
            }
        }
        else
        {
            currentTarget = null;
        }
    }

    protected override void PerformAttack()
    {
        if (owner == null || muzzlePoint == null || currentTarget == null || currentTarget.IsDead)
            return;

        Transform targetTransform = GetTransformFromIDamageable(currentTarget);
        if (targetTransform == null)
        {
            currentTarget = null;
            return;
        }

        if (Vector3.Distance(transform.position, targetTransform.position) <= maxRange)
        {
            FireBullet(targetTransform.position);
        }
    }

    private void FindClosestEnemy()
    {
        Vector3 origin = owner ? owner.transform.position : transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            maxRange,
            EnemyOverlapResults,
            enemyLayer
        );

        currentTarget = null;
        float closestSqrDist = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            var col = EnemyOverlapResults[i];
            if (col == null) continue;

            // ✅ 支持所有实现 IDamageable 的目标（包括 SimpleBossAI 和 EnemyController）
            if (col.TryGetComponent(out IDamageable target) && !target.IsDead)
            {
                // 如果是 EnemyController，尊重其 CanBeTargeted 标志
                if (target is EnemyController ec && !ec.CanBeTargeted)
                    continue;

                // 其他类型（如 Boss）默认可被瞄准
                float sqrDist = (col.transform.position - origin).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    currentTarget = target;
                }
            }
        }
    }
//锁定目标
    private void AimAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }
    }

    private void FireBullet(Vector3 targetPosition)
    {
        if (statSystem == null || muzzlePoint == null) return;

        IBullet bullet = BulletPool.Instance?.GetPlayerBullet(bulletTypeKey);
        if (bullet == null)
        {
            Debug.LogError($"BulletPool failed to get bullet with key: {bulletTypeKey}");
            return;
        }

        if (bullet is MonoBehaviour mb)
        {
            mb.transform.position = muzzlePoint.position;
            mb.transform.rotation = muzzlePoint.rotation;
            mb.gameObject.SetActive(true);
        }
        Vector3 shootDir=targetPosition-transform.position;
        float damage = statSystem.GetFinalValue(PlayerStatAttr.AttackDamage)*attkMagnification;
        float knockBackForce = statSystem.GetFinalValue(PlayerStatAttr.KnockBackForce) * 0.5f;
        Debug.Log("远程武器的伤害为："+damage);
        bullet.Initialize(damage, owner,knockBackForce,shootDir);
        bullet.OnShoot();
        AudioManager.instance?.Play("shot1");
    }

    // ✅ 辅助方法：从 IDamageable 获取 Transform
    private Transform GetTransformFromIDamageable(IDamageable target)
    {
        if (target is Component comp)
            return comp.transform;
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && owner != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.transform.position, maxRange);
        }
    }
}