using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float lifetime = 5f;
    public float knockBackForce = 5f; // 击退力
    
    private Vector3 direction;
    private float timer = 0f;
    private GameObject source; // 伤害来源

    public void SetDirection(Vector3 dir, GameObject sourceObject = null)
    {
        direction = dir.normalized;
        source = sourceObject;
        
        // 确保子弹朝向移动方向
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Start()
    {
        // 确保子弹有碰撞器和刚体
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>().radius = 0.1f;
            gameObject.AddComponent<SphereCollider>().isTrigger =false;

        }
        
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // 使用运动学刚体，避免物理碰撞干扰
        }
    }

    void Update()
    {
        // 移动
        transform.position += direction * speed * Time.deltaTime;
        
        // 更新计时器
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 跳过子弹来源和同阵营的物体
        if (source != null && other.gameObject == source) return;
        
        // 检测是否击中玩家
        if (other.CompareTag("Player"))
        {
            // 对玩家造成伤害
            Health damageable = other.GetComponent<Health>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            
            // 可以在这里添加击中特效
            Destroy(gameObject);
        }
    }
    
    // 可视化子弹轨迹（可选）
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 0.5f);
    }
}