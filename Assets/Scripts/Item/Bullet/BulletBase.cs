// BulletBase.cs
using UnityEngine;

public abstract class BulletBase : MonoBehaviour, IBullet
{
    [HideInInspector] public string poolKey; // 由 BulletPool 注入

    protected float damage;
    protected GameObject owner;
    protected bool isActive = false;
    protected float finalKnockBackForce;
    protected Vector3 shootDirection; // 固定方向，永不改变

    public virtual void Initialize(float damage, GameObject owner, float knockBackForce, Vector3 direction)
    {
        this.damage = damage;
        this.owner = owner;
        this.finalKnockBackForce = knockBackForce;
        this.shootDirection = direction.normalized;
        this.isActive = true;
        gameObject.SetActive(true);
    }

    public virtual void OnShoot()
    {
        // 子类可扩展
    }

    public virtual void OnReturnToPool()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    public string GetPoolKey() => poolKey;

    // 提供统一回收入口（子类调用）
    protected virtual void ReturnToPool()
    {
        BulletPool.Instance?.ReturnBullet(this);
    }

    // 统一命中处理（可选复用）
    protected virtual void ProcessHit(Collider other)
    {
        if (other == null || other.gameObject == owner) return;

        if (other.TryGetComponent(out IDamageable damageable) && !damageable.IsDead)
        {
            DamageInfo info = new DamageInfo
            {
                damage = this.damage,
                source = owner,
                knockBackDirection = shootDirection,
                knockBackForce = finalKnockBackForce
            };
            damageable.TakeDamage(info);
        }
    }
}