// EnemyStraightBullet.cs
using UnityEngine;

/// <summary>
/// 通用敌人投射物：直线飞行，命中即造成伤害并回收
/// 挂在敌人子弹 Prefab 上
/// </summary>
public class EnemyStraightBullet : BulletBase
{
    [Header("飞行参数")]
    [SerializeField] private float speed = 12f;        // 飞行速度
    [SerializeField] private float maxLifetime = 4f;    // 最大存活时间（防漏回收）

    private void OnEnable()
    {
        // 安全兜底：超时自动回收
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    private void Update()
    {
        if (!isActive) return;
        // 沿固定方向飞行（初始化时已确定 shootDirection）
        transform.position += shootDirection * (speed * Time.deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isActive || other == null || other.gameObject == owner)
            return;

        // 🔥 关键修改：不再依赖 IDamageable，而是直接找 Player Tag + Health
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null )
            {
                // 根据你的 Health 实际接口调整参数
                // 情况1：如果你的 Health.TakeDamage 接受 int
                playerHealth.TakeDamage((int)this.damage);

                // 情况2：如果你的 Health.TakeDamage 接受 DamageInfo（推荐）
                /*
                DamageInfo info = new DamageInfo
                {
                    damage = (int)this.damage,
                    source = owner,
                    knockBackDirection = shootDirection,
                    knockBackForce = finalKnockBackForce
                };
                playerHealth.TakeDamage(info);
                */

                ReturnToPool();
                return;
            }
        }

        // 可选：如果想让子弹穿墙/不打其他东西，这里不做任何事
        // 如果碰到非玩家物体（如墙壁），子弹继续飞行直到超时
    }

    public override void OnReturnToPool()
    {
        // 清理状态，防止残留
        CancelInvoke(); // 取消 lifetime 回收
        base.OnReturnToPool(); // 停用 GameObject
    }
}