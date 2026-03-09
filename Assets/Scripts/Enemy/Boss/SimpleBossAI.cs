using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class Vector3UnityEvent : UnityEvent<Vector3> { }

public class SimpleBossAI : MonoBehaviour, IDamageable
{
    [Header("References")]
    public Transform firePoint;

    [Header("Detection")]
    public float meleeRange = 3f;
    public float laserRange = 12f;
    public float maxChaseRange = 15f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 6f;
    public Transform playGround;

    [Header("Melee Attack")]
    public int meleeDamage = 20;
    public float meleeCooldown = 2f;
    public float meleeRadius = 2f;
    private float lastMeleeTime;

    [Header("Laser Attack")]
    public int laserDamage = 35;
    public float laserCooldown = 4f;
    public float laserWarningTime = 1.2f;
    public float laserLength = 15f;
    public LayerMask playerLayer;
    private float lastLaserTime;

    [Header("Sweep Laser Attack")]
    public int sweepDamage = 20;
    public int sweepLaserCount = 5;
    public float sweepAngle = 60f;
    public float sweepInterval = 0.15f;
    public float sweepWarningTime = 1.0f;
    public bool useSweepAttack = true;
    [Header("Sweep Preview")]
    public GameObject laserPreviewLinePrefab;
    private List<GameObject> activeWarningLines = new List<GameObject>(); // ✅ 统一管理预警线

    [Header("Dash Attack")]
    public float dashTriggerRange = 4f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.3f;
    public int dashDamage = 25;
    public float dashCooldown = 2.5f;
    private float lastDashTime;
    public ParticleSystem dashVFX;
    public AudioClip dashSFX;
    private AudioSource audioSource;

    [Header("死亡掉落")]
    public int bossExperience = 500;
    public int minOrbs = 10;
    public int maxOrbs = 20;
    public float dropRadius = 3f;

    [Header("Health")]
    public float maxHealth = 200f;
    private float currentHealth;

    [Header("残影设置")]
    public GameObject DashEffectPrefab;
    public int DEffectNum = 5;
    private float afterimageTimer = 0f;
    private float afterimageInterval = 0f;

    [Header("狂暴阶段（血量 ≤ 50%）")]
    public float enragedMoveSpeed = 4f;
    public float enragedDashCooldown = 1.5f;
    public float enragedDashSpeed = 15f;
    private bool wasEnraged = false;
    private bool IsEnraged => currentHealth <= maxHealth * 0.5f;
    private float CurrentMoveSpeed => IsEnraged ? enragedMoveSpeed : moveSpeed;
    private float CurrentDashSpeed => IsEnraged ? enragedDashSpeed : dashSpeed;
    private float CurrentDashCooldown => IsEnraged ? enragedDashCooldown : dashCooldown;

    [Header("Events - 技能播报")]
    public UnityEvent<Vector3> onDashStart = new Vector3UnityEvent();
    public UnityEvent onMeleeStart = new UnityEvent();
    public UnityEvent<Vector3> onLaserStart = new Vector3UnityEvent();
    public UnityEvent<Vector3> onSweepLaserStart = new Vector3UnityEvent();

    [Header("Laser Visuals (Game View)")]
    public GameObject laserBeamPrefab;

    // ✅ 新增：追踪运行时生成的对象
    private List<GameObject> activeLaserBeams = new List<GameObject>();

    public event Action OnBossDied;
    private Transform player => GameManager.Instance?.player;
    private Rigidbody rb;
    private Bounds bossMoveBounds;

    public bool IsDead => currentHealth <= 0;

    // ===== IDamageable Implementation =====
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;

        currentHealth -= damageInfo.damage;
        wasEnraged = IsEnraged;

        Debug.Log($"Boss 受到 {damageInfo.damage} 点伤害，剩余 {currentHealth}", this);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // ✅ 安全清理所有运行时生成的视觉对象（不是 prefab！）
        foreach (var obj in activeWarningLines)
        {
            if (obj != null) Destroy(obj);
        }
        foreach (var obj in activeLaserBeams)
        {
            if (obj != null) Destroy(obj);
        }

        activeWarningLines.Clear();
        activeLaserBeams.Clear();

        Debug.Log("Boss 被击败！", this);
        OnBossDied?.Invoke();
        DropExperience();
        Destroy(gameObject);
    }

    void Awake()
    {
        if (playGround == null)
        {
            GameObject groundObj = GameObject.FindGameObjectWithTag("PlayGround");
            if (groundObj != null)
                playGround = groundObj.transform;
            else
                Debug.LogWarning("未找到 Tag 为 'PlayGround' 的对象！");
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Boss 必须有 Rigidbody 组件！");
            enabled = false;
            return;
        }
        rb.freezeRotation = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Collider bossCol = GetComponent<Collider>();
            Collider playerCol = playerObj.GetComponent<Collider>();
            if (bossCol != null && playerCol != null)
                Physics.IgnoreCollision(bossCol, playerCol, true);
        }

        if (dashSFX != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        if (playGround != null)
        {
            Collider groundCollider = playGround.GetComponent<Collider>();
            if (groundCollider != null)
                bossMoveBounds = groundCollider.bounds;
            else
            {
                Debug.LogError("PlayGround missing Collider!", this);
                bossMoveBounds = new Bounds(transform.position, Vector3.one * 50f);
            }
        }
        else
        {
            Debug.LogWarning("PlayGround not assigned! Using fallback area.", this);
            bossMoveBounds = new Bounds(transform.position, Vector3.one * 50f);
        }
    }

    void Update()
    {
        if (player == null || isAttacking || IsDead) return; // ✅ 检查死亡

        float dist = Vector3.Distance(transform.position, player.position);
        bool canMelee = Time.time - lastMeleeTime > meleeCooldown;
        bool canLaser = Time.time - lastLaserTime > laserCooldown;
        bool canDash = Time.time - lastDashTime > CurrentDashCooldown;

        if (dist <= dashTriggerRange && canDash)
        {
            StartCoroutine(DashAttack());
        }
        else if (dist <= meleeRange && canMelee)
        {
            StartCoroutine(MeleeAttack());
        }
        else if (dist > meleeRange && dist <= laserRange && canLaser)
        {
            bool shouldUseSweep = useSweepAttack && (currentHealth / maxHealth > 0.5f);

            if (!IsEnraged && shouldUseSweep)
            {
                StartCoroutine(SweepLaserAttackSequence());
            }
            else
            {
                StartCoroutine(LaserAttackSequence());
            }

            lastLaserTime = Time.time;
        }
    }

    bool isAttacking = false;
    bool isDashing = false;
    Vector3 dashDirection;
    float dashTimer;

    void FixedUpdate()
    {
        if (player == null || IsDead) return;

        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= dashDuration)
            {
                isDashing = false;
            }
            else
            {
                Vector3 nextPos = transform.position + transform.forward * CurrentDashSpeed * Time.fixedDeltaTime;
                if (playGround != null)
                    nextPos = playGround.GetComponent<Collider>().bounds.ClosestPoint(nextPos);
                rb.MovePosition(nextPos);

                afterimageTimer += Time.fixedDeltaTime;
                if (afterimageTimer >= afterimageInterval && DashEffectPrefab != null)
                {
                    afterimageTimer = 0f;
                    GameObject effect = Instantiate(DashEffectPrefab, transform.position, transform.rotation);
                    Destroy(effect, 1f);
                }

                Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f, playerLayer);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        Health health = hit.GetComponent<Health>();
                        if (health != null)
                            health.TakeDamage(dashDamage);
                    }
                }
            }
        }
        else if (!isAttacking)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= maxChaseRange && dist > meleeRange)
            {
                Vector3 flatDir = player.position - transform.position;
                flatDir.y = 0;
                if (flatDir.magnitude > 0.1f)
                {
                    flatDir.Normalize();
                    Quaternion targetRot = Quaternion.LookRotation(flatDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);

                    Vector3 nextPos = transform.position + transform.forward * CurrentMoveSpeed * Time.fixedDeltaTime;
                    if (playGround != null)
                        nextPos = playGround.GetComponent<Collider>().bounds.ClosestPoint(nextPos);
                    rb.MovePosition(nextPos);
                }
            }
        }
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
        
            // ✅ 正确：从单位圆获取 (x, z) 平面上的随机点
            Vector2 circleOffset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPos = center + new Vector3(circleOffset.x, 0.5f, circleOffset.y); // 👈 .y → Z

            ExperienceOrbSpawner.Instance.Spawn(spawnPos, exp);
        }

        Debug.Log($"Boss dropped {bossExperience} experience in {orbCount} orbs.");
    }

    IEnumerator DashAttack()
    {
        if (player == null || IsDead) yield break; // ✅

        isAttacking = true;
        const int totalDashes = 3;
        const float betweenDashDelay = 0.25f;

        for (int i = 0; i < totalDashes; i++)
        {
            if (player == null || IsDead) break;

            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0;

            if (directionToPlayer.magnitude > 0.1f)
            {
                dashDirection = directionToPlayer.normalized;
                transform.rotation = Quaternion.LookRotation(dashDirection);
            }
            else
            {
                dashDirection = transform.forward;
            }

            onDashStart?.Invoke(dashDirection);

            if (dashVFX != null) dashVFX.Play();
            if (audioSource != null && dashSFX != null)
                audioSource.PlayOneShot(dashSFX);

            afterimageInterval = dashDuration / Mathf.Max(1, DEffectNum);
            afterimageTimer = 0f;

            isDashing = true;
            dashTimer = 0f;

            yield return new WaitForSeconds(dashDuration);
            isDashing = false;

            if (i < totalDashes - 1)
                yield return new WaitForSeconds(betweenDashDelay);
        }

        lastDashTime = Time.time;
        yield return new WaitForSeconds(0.15f);
        isAttacking = false;
    }

    IEnumerator MeleeAttack()
    {
        if (IsDead) yield break; // ✅

        isAttacking = true;
        lastMeleeTime = Time.time;
        onMeleeStart?.Invoke();
        yield return new WaitForSeconds(0.3f);

        Collider[] hits = Physics.OverlapSphere(transform.position, meleeRadius, playerLayer);
        foreach (var hit in hits)
        {
            Health health = hit.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(meleeDamage);
                Debug.Log($"Boss hit {hit.name}");
            }
        }

        yield return new WaitForSeconds(0.7f);
        isAttacking = false;
    }

    IEnumerator LaserAttackSequence()
    {
        if (player == null || IsDead) // ✅
        {
            isAttacking = false;
            yield break;
        }

        isAttacking = true;
        lastLaserTime = Time.time;

        Vector3 flatDir = (player.position - transform.position).normalized;
        flatDir.y = 0;
        transform.rotation = Quaternion.LookRotation(flatDir);

        GameObject warningLine = null;
        if (laserPreviewLinePrefab != null)
        {
            Vector3 origin = GetLaserOrigin();
            Vector3 targetPos = player.position + Vector3.up * 1.0f;
            warningLine = Instantiate(laserPreviewLinePrefab, origin, Quaternion.identity);
            UpdateLaserPreview(warningLine, origin, targetPos);
            warningLine.SetActive(true);
            activeWarningLines.Add(warningLine); // ✅ 注册
        }

        float warningElapsed = 0f;
        while (warningElapsed < laserWarningTime)
        {
            if (IsDead) // ✅ 中途死亡立即退出
            {
                if (warningLine != null) Destroy(warningLine);
                yield break;
            }

            if (warningLine != null && player != null)
            {
                UpdateLaserPreview(warningLine, GetLaserOrigin(), player.position + Vector3.up * 1.0f);
            }
            warningElapsed += Time.deltaTime;
            yield return null;
        }

        if (IsDead) // ✅ 再次检查
        {
            if (warningLine != null) Destroy(warningLine);
            yield break;
        }

        Vector3 finalAimPosition = player.position + Vector3.up * 1.0f;
        onLaserStart?.Invoke((finalAimPosition - GetLaserOrigin()).normalized);

        if (warningLine != null)
        {
            activeWarningLines.Remove(warningLine);
            Destroy(warningLine);
        }

        yield return new WaitForSeconds(0.3f);

        if (!IsDead) // ✅ 仅在存活时发射
            FireLaserAt(finalAimPosition);

        yield return new WaitForSeconds(0.1f);
        isAttacking = false;
    }

    IEnumerator SweepLaserAttackSequence()
    {
        if (player == null || IsDead) yield break; // ✅

        isAttacking = true;
        lastLaserTime = Time.time;
        Vector3 lockedPlayerPosition = player.position;

        Vector3 flatDir = (lockedPlayerPosition - transform.position).normalized;
        flatDir.y = 0;
        transform.rotation = Quaternion.LookRotation(flatDir);

        ClearPreviewLines();
        Vector3 centerDir = flatDir;
        float halfAngle = sweepAngle * 0.5f;

        for (int i = 0; i < sweepLaserCount; i++)
        {
            if (IsDead) break; // ✅

            float t = (float)i / Mathf.Max(1, sweepLaserCount - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = (rot * centerDir).normalized;

            Vector3 origin = GetLaserOrigin();
            Vector3 end = origin + dir * laserLength;

            GameObject lineObj = Instantiate(laserPreviewLinePrefab, origin, Quaternion.identity);
            UpdateLaserPreview(lineObj, origin, end);
            lineObj.SetActive(true);
            activeWarningLines.Add(lineObj); // ✅
        }

        if (IsDead)
        {
            ClearPreviewLines();
            yield break;
        }

        yield return new WaitForSeconds(sweepWarningTime);

        if (IsDead)
        {
            ClearPreviewLines();
            yield break;
        }

        ClearPreviewLines();
        onSweepLaserStart?.Invoke(centerDir);
        yield return new WaitForSeconds(0.25f);

        if (!IsDead)
        {
            for (int i = 0; i < sweepLaserCount; i++)
            {
                if (IsDead) break;
                float t = (float)i / Mathf.Max(1, sweepLaserCount - 1);
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                FireSweepLaserAtDirection(centerDir, angle);
                yield return new WaitForSeconds(sweepInterval);
            }
        }

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    private void ClearPreviewLines()
    {
        foreach (var line in activeWarningLines)
            if (line != null) Destroy(line);
        activeWarningLines.Clear();
    }

    private Vector3 GetLaserOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.2f;
    }

    void UpdateLaserPreview(GameObject previewObj, Vector3 start, Vector3 end)
    {
        if (previewObj == null) return;
        Vector3 dir = end - start;
        float length = dir.magnitude;
        if (length < 0.01f) return;

        previewObj.transform.position = start + dir * 0.5f;
        previewObj.transform.LookAt(end);
        Vector3 originalScale = laserPreviewLinePrefab ? laserPreviewLinePrefab.transform.localScale : Vector3.one;
        previewObj.transform.localScale = new Vector3(originalScale.x, originalScale.y, length);
    }

    void FireLaserAt(Vector3 targetPosition)
    {
        if (IsDead) return; // ✅

        Vector3 laserOrigin = GetLaserOrigin();
        Vector3 direction = (targetPosition - laserOrigin).normalized;

        direction += new Vector3(
            Random.Range(-0.02f, 0.02f),
            Random.Range(-0.02f, 0.02f),
            Random.Range(-0.02f, 0.02f)
        );
        direction.Normalize();

        Vector3 endPos = laserOrigin + direction * laserLength;

        if (Physics.Raycast(laserOrigin, direction, out RaycastHit hit, laserLength, playerLayer))
        {
            endPos = hit.point;
            if (hit.collider.CompareTag("Player"))
            {
                Health health = hit.collider.GetComponentInParent<Health>();
                health?.TakeDamage(laserDamage);
                Debug.Log($"🔥 激光命中！伤害: {laserDamage}", this);
            }
        }

        Debug.DrawLine(laserOrigin, endPos, Color.blue, 0.2f);
        ShowLaserBeam(laserOrigin, endPos, Color.blue, 0.15f);
    }

    void FireSweepLaserAtDirection(Vector3 baseDirection, float angleOffset)
    {
        if (IsDead) return; // ✅

        Vector3 sweepOrigin = GetLaserOrigin();
        Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
        Vector3 direction = rotation * baseDirection;
        direction.Normalize();

        Vector3 endPos = sweepOrigin + direction * laserLength;

        if (Physics.Raycast(sweepOrigin, direction, out RaycastHit hit, laserLength, playerLayer))
        {
            endPos = hit.point;
            if (hit.collider.CompareTag("Player"))
            {
                Health health = hit.collider.GetComponentInParent<Health>();
                health?.TakeDamage(sweepDamage);
                Debug.Log($"[Sweep Laser] 命中玩家！伤害: {sweepDamage}", this);
            }
        }

        Debug.DrawLine(sweepOrigin, endPos, Color.red, 0.2f);
        ShowLaserBeam(sweepOrigin, endPos, Color.red, 0.12f);
    }

    void ShowLaserBeam(Vector3 start, Vector3 end, Color color, float duration)
    {
        AudioManager.instance?.Play("laser");
        if (laserBeamPrefab == null || IsDead) return;

        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length < 0.01f) return;

        GameObject beam = Instantiate(laserBeamPrefab, start + direction * 0.5f, Quaternion.identity);
        beam.transform.LookAt(end);
        

        Vector3 origScale = laserBeamPrefab.transform.localScale;
        beam.transform.localScale = new Vector3(origScale.x, origScale.y, length);

        Renderer rend = beam.GetComponent<Renderer>();
        if (rend != null && rend.material.HasProperty("_Color"))
        {
            rend.material.color = color;
        }

        Destroy(beam, duration);
        activeLaserBeams.Add(beam); // ✅ 注册
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, laserRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, dashTriggerRange);

        if (player != null)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            toPlayer.y = 0;
            float half = sweepAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -half, 0) * toPlayer;
            Vector3 rightDir = Quaternion.Euler(0, half, 0) * toPlayer;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + leftDir * laserLength);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * laserLength);
        }
    }
    public float GetDefense() =>  0f;
    public float GetDamageReduction() =>  0f;
}