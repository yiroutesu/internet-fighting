using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//[System.Serializable]
//public class Vector3UnityEvent : UnityEvent<Vector3> { }

public class BossContext : MonoBehaviour, IDamageable
{
    [Header("References")]
    public CubeController anim;
    public Transform player;
    public AudioSource audioSrc;
    public Transform firePoint;
    public ParticleSystem dashVFX;
    public GameObject DashEffectPrefab;
    public Transform playGround;

    [Header("Health")]
    public float maxHp = 10000;
    public float hp;

    [Header("Detection Ranges - 在Scene中用Gizmos可视化")]
    [Tooltip("追踪范围（黄色）")]
    public float trackRange = 10f;
    [Tooltip("近战攻击范围（红色）")]
    public float atkRange = 3f;
    [Tooltip("近战检测范围（暗红色）")]
    public float meleeRange = 3f;
    [Tooltip("激光攻击范围（蓝色）")]
    public float laserRange = 12f;
    [Tooltip("冲刺触发范围（紫色）")]
    public float dashTriggerRange = 4f;
    [Tooltip("最大追逐范围（绿色）")]
    public float maxChaseRange = 15f;

    [Header("Movement")]
    public float trackSpeed = 4f;
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 6f;
    public float cubeRotationInterval = 1.5f;

    [Header("Melee Attack")]
    public int meleeDamage = 20;
    public float meleeCooldown = 2f;
    public float meleeRadius = 2f;
    [Header("Bullet Settings")]  // 新增：子弹设置
    public GameObject bulletPrefab;  // 子弹预制体
    public float bulletSpeed = 10f;  // 子弹速度
    public int bulletDamage = 10;    // 子弹伤害
    public int bulletCount = 8;      // 每次发射的子弹数量（一圈）

    [Header("Dash Attack")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.3f;
    public int dashDamage = 25;
    public float dashCooldown = 2.5f;
    public int dashCount = 3;
    public float betweenDashDelay = 0.25f;
    public AudioClip dashSFX;

    [Header("Laser Attack")]
    public LaserWarningController laserWarningController;
    public GameObject Laser;
    public int laserDamage = 35;
    public float laserCooldown = 4f;
    public float laserWarningTime = 1.2f;
    public float laserLength = 15f;
    public LayerMask playerLayer;

    [Header("Sweep Laser Attack")]
    public int sweepDamage = 20;
    public int sweepLaserCount = 5;
    public float sweepAngle = 60f;
    public float sweepInterval = 0.15f;
    public float sweepWarningTime = 1.0f;
    public bool useSweepAttack = true;

    [Header("残影设置")]
    public int DEffectNum = 5;

    [Header("狂暴阶段（血量 ≤ 50%）")]
    public float enragedMoveSpeed = 4f;
    public float enragedDashCooldown = 1.5f;
    public float enragedDashSpeed = 15f;

    [Header("死亡掉落")]
    public int bossExperience = 500;
    public int minOrbs = 10;
    public int maxOrbs = 20;
    public float dropRadius = 3f;
    public float dieDelay = 2f;

    [Header("Events - 技能播报")]
    public UnityEvent<Vector3> onDashStart = new Vector3UnityEvent();
    public UnityEvent onMeleeStart = new UnityEvent();
    public UnityEvent<Vector3> onLaserStart = new Vector3UnityEvent();
    public UnityEvent<Vector3> onSweepLaserStart = new Vector3UnityEvent();
    public UnityEvent OnBossDied = new UnityEvent();

    [Header("Gizmos显示设置")]
    public bool showGizmos = true;
    public bool showRangeLabels = true;
    [Range(0.1f, 0.5f)]
    public float gizmoOpacity = 0.3f;

    // Runtime variables
    [HideInInspector] public float lastMeleeTime;
    [HideInInspector] public float lastLaserTime;
    [HideInInspector] public float lastDashTime;
    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public bool isDashing = false;
    [HideInInspector] public Vector3 dashDirection;
    [HideInInspector] public float dashTimer;
    [HideInInspector] public float afterimageTimer = 0f;
    [HideInInspector] public float afterimageInterval = 0f;
    [HideInInspector] public bool wasEnraged = false;

    [HideInInspector] public List<GameObject> activeWarningLines = new List<GameObject>();
    [HideInInspector] public List<GameObject> activeLaserBeams = new List<GameObject>();

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Bounds bossMoveBounds;

    // Properties
    public Vector3 PlayerDir => player != null ? (player.position - transform.position).normalized : Vector3.zero;
    public float PlayerDist => player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
    public bool IsDead => hp <= 0;
    public bool IsEnraged => hp <= maxHp * 0.5f;
    public float CurrentMoveSpeed => IsEnraged ? enragedMoveSpeed : moveSpeed;
    public float CurrentDashSpeed => IsEnraged ? enragedDashSpeed : dashSpeed;
    public float CurrentDashCooldown => IsEnraged ? enragedDashCooldown : dashCooldown;

    // Cooldown checks
    public bool CanMelee => Time.time - lastMeleeTime > meleeCooldown;
    public bool CanLaser => Time.time - lastLaserTime > laserCooldown;
    public bool CanDash => Time.time - lastDashTime > CurrentDashCooldown;

    void Awake()
    {
        hp = maxHp;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.freezeRotation = true;

        if (playGround == null)
        {
            GameObject groundObj = GameObject.FindGameObjectWithTag("PlayGround");
            if (groundObj != null)
                playGround = groundObj.transform;
        }

        if (playGround != null)
        {
            Collider groundCollider = playGround.GetComponent<Collider>();
            if (groundCollider != null)
                bossMoveBounds = groundCollider.bounds;
        }

        if (dashSFX != null && audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 1f;
        }
    }

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;

        hp -= info.damage;
        Debug.Log($"Boss 受到 {info.damage} 点伤害，剩余 {hp}", this);

        if (hp <= 0)
        {
            GetComponent<BossFSM>()?.ChangeState<BossDieState>();
        }
        else if (hp / maxHp < 0.3f && !IsEnraged)
        {
            GetComponent<BossFSM>()?.ChangeState<BossPhase2State>();
        }
    }

    public void Die()
    {
        //if (IsDead) return;

        hp = 0;
        enabled = false;

        // 清理所有生成的视觉对象
        ClearPreviewLines();
        foreach (var obj in activeLaserBeams)
            if (obj != null) Destroy(obj);
        activeLaserBeams.Clear();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("Boss 被击败！", this);
        OnBossDied?.Invoke();
        DropExperience();
        Destroy(gameObject, dieDelay);
    }

    private void DropExperience()
    {
        if (bossExperience <= 0 || ExperienceOrbSpawner.Instance == null) return;

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        int baseExpPerOrb = bossExperience / orbCount;
        int remainder = bossExperience % orbCount;
        Vector3 center = transform.position;

        for (int i = 0; i < orbCount; i++)
        {
            int exp = baseExpPerOrb + (i < remainder ? 1 : 0);
            Vector2 circleOffset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPos = center + new Vector3(circleOffset.x, 0.5f, circleOffset.y);
            ExperienceOrbSpawner.Instance.Spawn(spawnPos, exp);
        }

        Debug.Log($"Boss dropped {bossExperience} experience in {orbCount} orbs.");
    }

    public void ClearPreviewLines()
    {
        foreach (var line in activeWarningLines)
            if (line != null) Destroy(line);
        activeWarningLines.Clear();
    }

    public Vector3 GetLaserOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.2f;
    }
    public float GetDefense() =>  0f;
    public float GetDamageReduction() => 0f;

    public void ShootBullets()
    {
        if (bulletPrefab == null)
        { 
        return;
        } 

        for (int i = 0; i < bulletCount; i++)
        {
            // 计算子弹方向（在水平面上均匀分布）
            float angle = i * (360f / bulletCount);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // 创建子弹
            Vector3 spawnPosition = transform.position-new Vector3(0,0.5f,0);
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(direction));

            // 设置子弹属性
            if (bullet.TryGetComponent<BossBullet>(out var bulletComponent))
            {
                bulletComponent.damage = bulletDamage;
                bulletComponent.speed = bulletSpeed;
                bulletComponent.knockBackForce = 3f; // 可以配置化
                bulletComponent.SetDirection(direction, gameObject); // 传递伤害来源
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 center = transform.position;
        center.y = 0.1f; // 稍微抬高避免与地面重叠

        // 1. 最大追逐范围（最外层 - 绿色半透明）
        Gizmos.color = new Color(0f, 1f, 0f, gizmoOpacity);
        Gizmos.DrawSphere(center, maxChaseRange);

        // 2. 激光攻击范围（蓝色半透明）
        Gizmos.color = new Color(0f, 0.5f, 1f, gizmoOpacity);
        Gizmos.DrawSphere(center, laserRange);

        // 3. 追踪范围（黄色半透明）
        Gizmos.color = new Color(1f, 1f, 0f, gizmoOpacity);
        Gizmos.DrawSphere(center, trackRange);

        // 4. 冲刺触发范围（紫色实线）
        Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.8f);
        Gizmos.DrawWireSphere(center, dashTriggerRange);

        // 5. 近战范围（红色实线）
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(center, meleeRange);

        // 6. 攻击范围（暗红色实线，比meleeRange小）
        Gizmos.color = new Color(0.7f, 0f, 0f, 1f);
        Gizmos.DrawWireSphere(center, atkRange);

        // 绘制标签（如果需要）
        if (showRangeLabels)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 10;
            style.alignment = TextAnchor.MiddleCenter;

            // 在对应范围位置绘制标签
            DrawRangeLabel(center, atkRange, "攻击", Color.red);
            DrawRangeLabel(center, meleeRange, "近战", new Color(1f, 0.3f, 0.3f));
            DrawRangeLabel(center, dashTriggerRange, "冲刺", Color.magenta);
            DrawRangeLabel(center, trackRange, "追踪", Color.yellow);
            DrawRangeLabel(center, laserRange, "激光", Color.cyan);
            DrawRangeLabel(center, maxChaseRange, "最大", Color.green);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        center.y = 0.1f;

        // 用更明显的颜色绘制被选中时的范围
        if (showGizmos)
        {
            // 激光范围（选中时更明显）
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.5f);
            Gizmos.DrawSphere(center, laserRange);

            // 冲刺范围（选中时更明显）
            Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
            Gizmos.DrawWireSphere(center, dashTriggerRange);

            // 绘制朝向线
            if (player != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(center, player.position);
            }
        }
    }

    void DrawRangeLabel(Vector3 center, float radius, string label, Color color)
    {
        Vector3 labelPos = center + Vector3.right * radius;
        labelPos.y = 0.5f;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;

        UnityEditor.Handles.Label(labelPos, $"{label}: {radius}m", style);
    }
#endif
}