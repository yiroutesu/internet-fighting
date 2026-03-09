using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class damge : MonoBehaviour
{
    [SerializeField] private DamageSO damageSO;
    [Header("打完怎么办")]
    [SerializeField] private bool destroyOnHit = true; // true = 子弹模式；false = 持续伤害
    [SerializeField]
    // 进入瞬间触发一次
    void OnTriggerEnter(Collider other)
    {
        // 1. 层过滤
        if (((1 << other.gameObject.layer) & damageSO.targetLayers) == 0) return;

        // 2. 拿 Health 组件
        Health health = other.GetComponent<Health>();
        if (health == null) return;

        // 3. 扣血
        health.TakeDamage(damageSO.damage);

        // 播放命中效果
        if (damageSO.onHitFx != null)
            Instantiate(damageSO.onHitFx, transform.position, Quaternion.identity);
        if (damageSO.onHitSfx != null)
            AudioSource.PlayClipAtPoint(damageSO.onHitSfx, transform.position);

        // 4. 自毁（可选）
        if (destroyOnHit)
            EnemyPool.Instance.Return(gameObject);
    }

    // 如果想“站在上面持续掉血”，用 OnTriggerStay
    /*
    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;
        Health health = other.GetComponent<Health>();
        health?.TakeDamage(damage);   // 每物理帧一次，最好加冷却
    }
    */
}
