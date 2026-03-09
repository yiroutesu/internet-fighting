// IBullet.cs
using UnityEngine;

public interface IBullet
{
    void Initialize(float damage, GameObject owner, float knockBackForce, Vector3 direction);
    void OnShoot();
    void OnReturnToPool();
    string GetPoolKey(); // 新增：用于对象池精准归还
}