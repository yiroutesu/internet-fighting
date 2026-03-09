// EnemyPistolBullet.cs
using UnityEngine;

/// <summary>
/// 敌人通用投射物（仿 PistolBullet）
/// 挂在 Enemy 子弹 Prefab 上，配合 BulletPool 使用
/// </summary>
public class EnemyPistolBullet : BulletBase
{
    [Header("飞行参数")]
    public float speed = 15f;        // 建议比玩家慢一点
    public float lifetime = 3f;      // 最大存活时间

    private Coroutine timeoutCoroutine;

    private void OnEnable()
    {
        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(TimeoutCoroutine());
    }

    private System.Collections.IEnumerator TimeoutCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        if (isActive)
            ReturnToPool();
    }

    void Update()
    {
        if (isActive)
        {
            // ✅ 严格沿初始化方向飞行，不旋转
            transform.position += shootDirection * speed * Time.deltaTime;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // 防御性检查
        if (!isActive || other == null || other.gameObject == owner)
            return;

        // ✅ 只攻击 IDamageable（通常是玩家或友军）
        if (other.TryGetComponent(out IDamageable damageable) && !damageable.IsDead)
        {
            DamageInfo info = new DamageInfo
            {
                damage = this.damage,
                source = owner, // 敌人 GameObject
                knockBackDirection = shootDirection,
                knockBackForce = finalKnockBackForce
            };
            damageable.TakeDamage(info);

            ReturnToPool(); // 单次命中即销毁
        }
    }

    // ✅ 关键：通过 BulletPool 回收（不是 Destroy）
    protected override void ReturnToPool()
    {
        BulletPool.Instance?.ReturnBullet(this);
    }
}