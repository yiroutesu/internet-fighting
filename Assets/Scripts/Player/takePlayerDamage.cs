using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class takePlayer : MonoBehaviour
{
    public int damage;

    void OnTriggerEnter(Collider other)
    {

        // 检测是否击中玩家
        if (other.CompareTag("Player"))
        {
            // 对玩家造成伤害
            Health damageable = other.GetComponent<Health>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            
        }
    }
}
