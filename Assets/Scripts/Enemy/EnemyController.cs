using System;
using System.Collections;
using SIGame.Enums;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    // 不再从 Inspector 配置！完全由生成器注入
    private EnemyData _enemyData;
    private string _enemyId;
    private SpawnPool _spawnPool;

    // 动态状态
    private float _currentHealth;
    private float _lastDamageTime;
    private bool _isDead = false;
    public float postSpawnDelay = 0.2f; // 初始化延迟时间
    public bool IsDead => _isDead;
    public EnemyData EnemyData => _enemyData;
    private EnemyStats _runtimeStats; // 运行时实际使用的属性（含波次成长）
    public EnemyStats RuntimeStats => _runtimeStats; // 可选：暴露给调试或 UI

    private int attackLockCount = 0;
    public const int MAX_CONCURRENT_ATTACKS = 1;
    private bool _isActive = false; // ← 新增：控制是否执行行为逻辑
    private float knockTimer = 0.2f; // 击退状态计时器
    private Rigidbody rb;
    public bool CanBeTargeted => !_isDead && attackLockCount < MAX_CONCURRENT_ATTACKS;

    // ===== 新增：就绪事件（可选）=====
    /// <summary>
    /// 当敌人完成延迟初始化并准备就绪时触发（仅一次）
    /// </summary>
    public event Action<EnemyController> OnReady;

    // ===== 新增：射击相关字段 =====
    private float _lastShootTime = 0f;
    private Coroutine _shootingRoutine;

    // === 攻击锁方法（保持不变）===
    public static bool TryLock(EnemyController enemy)
    {
        if (enemy == null || enemy._isDead) return false;
        if (enemy.attackLockCount >= MAX_CONCURRENT_ATTACKS) return false;
        enemy.AddAttackLock();
        return true;
    }

    public static void ReleaseLock(EnemyController enemy)
    {
        if (enemy != null)
        {
            enemy.RemoveAttackLock();
        }
    }

    /// <summary>
    /// 由 EnemySpawner 在生成时调用（从对象池取出后）
    /// </summary>
    public void OnSpawned(string enemyId, SpawnPool spawnPool, EnemyData enemyData)
    {
        _enemyId = enemyId;
        _spawnPool = spawnPool;
        _enemyData = enemyData;

        // 👇 关键：计算运行时属性（含波次成长）
        ComputeRuntimeStats();

        // 初始化状态（使用 _runtimeStats）
        Initialize();

        if (postSpawnDelay > 0f)
            StartCoroutine(ActivateAfterDelay(postSpawnDelay));
        else
            Activate();

        _spawnPool?.IncrementAlive(_enemyId, this);
    }

    private void ComputeRuntimeStats()
    {
        var src = _enemyData.stats;
        _runtimeStats = new EnemyStats(); // 深拷贝基础值

        // 手动复制所有字段（避免引用污染）
        _runtimeStats.enemyName = src.enemyName;
        _runtimeStats.tint = src.tint;
        _runtimeStats.scoreValue = src.scoreValue;
        _runtimeStats.experienceValue = src.experienceValue;
        _runtimeStats.isBoss = src.isBoss;
        _runtimeStats.maxAlive = src.maxAlive;
        _runtimeStats.spawnWeight = src.spawnWeight;
        _runtimeStats.contactDamageCooldown = src.contactDamageCooldown;
        _runtimeStats.defense = src.defense;
        _runtimeStats.damageReduction = src.damageReduction;
        _runtimeStats.canShoot = src.canShoot;
        _runtimeStats.bulletSubKey = src.bulletSubKey;
        _runtimeStats.shootInterval = src.shootInterval;
        _runtimeStats.shootRange = src.shootRange;
        _runtimeStats.shootPoint = src.shootPoint;
        _runtimeStats.bulletKnockBackForce = src.bulletKnockBackForce;

        // 👇 应用波次成长（仅普通敌人）
        if (!src.isBoss)
        {
            int currentWave = WaveManager.Instance?.currentWave ?? 1;
            var growth = WaveManager.Instance?.growthCurve;

            if (growth != null)
            {
                _runtimeStats.maxHealth = src.maxHealth * growth.health.Evaluate(currentWave);
                _runtimeStats.damage = src.damage * growth.damage.Evaluate(currentWave);
                _runtimeStats.moveSpeed = src.moveSpeed * growth.speed.Evaluate(currentWave);
                _runtimeStats.defense = src.defense * growth.defense.Evaluate(currentWave);

                // 百分比免伤：限制上限（防无敌）
                float dr = src.damageReduction * growth.damageReduction.Evaluate(currentWave);
                _runtimeStats.damageReduction = Mathf.Clamp01(dr);
            }
            else
            {
                // 无成长配置：使用原始值
                _runtimeStats.maxHealth = src.maxHealth;
                _runtimeStats.damage = src.damage;
                _runtimeStats.moveSpeed = src.moveSpeed;
            }
        }
        else
        {
            // Boss：完全使用原始值
            _runtimeStats.maxHealth = src.maxHealth;
            _runtimeStats.damage = src.damage;
            _runtimeStats.moveSpeed = src.moveSpeed;
        }

        if (WaveManager.Instance != null)
        {
            Debug.Log($"[{_runtimeStats.enemyName}] Wave {WaveManager.Instance.currentWave} → HP: {_runtimeStats.maxHealth:F0}, DMG: {_runtimeStats.damage:F1}, SPD: {_runtimeStats.moveSpeed:F2}");
        }
    }

    private void Initialize()
    {
        if (_enemyData == null || _runtimeStats == null)
        {
            Debug.LogError("EnemyController: Missing data!");
            return;
        }

        _currentHealth = _runtimeStats.maxHealth;
        _isDead = false;
        _lastDamageTime = 0f;
        attackLockCount = 0;
        _isActive = false;
        _lastShootTime = 0f;

        rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = _runtimeStats.tint;
    }

    private IEnumerator ActivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Activate();
    }

    private void Activate()
    {
        if (_isDead) return;
        _isActive = true;

        // 启动射击协程（如果配置了）
        if (_runtimeStats.canShoot && !string.IsNullOrEmpty(_runtimeStats.bulletSubKey))
        {
            _shootingRoutine = StartCoroutine(ShootingRoutine());
        }

        OnReady?.Invoke(this);
    }

    private void Update()
    {
        if (!_isActive || _isDead) return;
        // 可在此添加动画/特效等每帧逻辑（目前为空）
    }

    private void FixedUpdate()
    {
        if (knockTimer > 0)
        {
            knockTimer -= Time.fixedDeltaTime;
            return;
        }

        if (GameManager.Instance?.player != null)
        {
            // 所有敌人都朝向玩家（用于瞄准或近战方向）
            transform.forward = WherePlayer();

            if (_runtimeStats.canShoot)
            {
                // 远程敌人：不移动
                rb.velocity = Vector3.zero;
            }
            else
            {
                // 近战敌人：正常移动
                Vector3 moveDirection = new Vector3(transform.forward.x, 0, transform.forward.z);
                rb.velocity = moveDirection.normalized * _runtimeStats.moveSpeed;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive || _isDead) return;

        if (other.CompareTag("Player"))
        {
            // 远程敌人不进行接触攻击
            if (_runtimeStats.canShoot) 
                return;

            if (Time.time - _lastDamageTime > _runtimeStats.contactDamageCooldown)
            {
                Health playerHealth = other.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage((int)_runtimeStats.damage);
                    _lastDamageTime = Time.time;
                }
            }
        }
    }

    // ===== 射击逻辑 =====
    private IEnumerator ShootingRoutine()
    {
        while (_isActive && !_isDead)
        {
            if (CanShootAtPlayer())
            {
                Shoot();
            }
            yield return new WaitForSeconds(_runtimeStats.shootInterval);
        }
    }

    private bool CanShootAtPlayer()
    {
        var player = GameManager.Instance?.player;
        if (player == null) return false;
        if (Time.time - _lastShootTime < _runtimeStats.shootInterval) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;
        return toPlayer.magnitude <= _runtimeStats.shootRange;
    }

    private void Shoot()
    {
        if (!_runtimeStats.canShoot || string.IsNullOrEmpty(_runtimeStats.bulletSubKey)) 
            return;

        _lastShootTime = Time.time;

        Vector3 shootPos = _runtimeStats.shootPoint != null 
            ? _runtimeStats.shootPoint.position 
            : transform.position;

        Vector3 direction = WherePlayer();

        IBullet bullet = BulletPool.Instance?.GetEnemyBullet(_runtimeStats.bulletSubKey);
        if (bullet == null)
        {
            Debug.LogWarning($"[EnemyController] Failed to get enemy bullet: {_runtimeStats.bulletSubKey}");
            return;
        }

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(_runtimeStats.damage));
        bullet.Initialize(
            damage: finalDamage,
            owner: gameObject,
            knockBackForce: _runtimeStats.bulletKnockBackForce,
            direction: direction
        );

        if (bullet is MonoBehaviour mb)
        {
            mb.transform.position = shootPos;
            mb.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        bullet.OnShoot();
    }

    // ===== 其他方法保持不变 =====
    public void TakeDamage(DamageInfo info)
    {
        if (_isDead) return;
        _currentHealth -= info.damage;
        ApplyKonckBack(info);
        Vector3 popupPos = transform.position + Vector3.up * 1.5f;
        DamageTextPool.Instance.Show(popupPos, info.damage, info.isCritical);
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKonckBack(DamageInfo info)
    {
        if (knockTimer > 0) return;
        Vector3 dir = info.knockBackDirection.normalized;
        knockTimer = 0.2f;
        rb.AddForce(dir * info.knockBackForce, ForceMode.Impulse);
    }

    private void Die()
    {
        _isDead = true;
        GameManager.Instance?.AddScore(_enemyData.stats.scoreValue);
        
        if (_enemyData.stats.experienceValue > 0)
        {
            ExperienceOrbSpawner.Instance?.Spawn(
                transform.position + Vector3.up * 0.5f,
                _enemyData.stats.experienceValue
            );
        }

        rb.velocity = Vector3.zero;
        EnemyPool.Instance?.Return(gameObject);
        if (_spawnPool != null && !string.IsNullOrEmpty(_enemyId))
        {
            _spawnPool.DecrementAlive(_enemyId, this);
        }
    }

    void OnDestroy()
    {
        if (_spawnPool != null && !string.IsNullOrEmpty(_enemyId))
        {
            _spawnPool.DecrementAlive(_enemyId, this);
        }
    }

    public void AddAttackLock()
    {
        if (!_isDead)
        {
            attackLockCount++;
        }
    }

    public void RemoveAttackLock()
    {
        if (attackLockCount > 0)
        {
            attackLockCount--;
        }
    }

    public Vector3 WherePlayer()
    {
        Vector3 direction = GameManager.Instance.player.position - transform.position;
        direction.y = 0;
        return direction.normalized;
    }

    public void ForceDie()
    {
        if (_isDead) return;
        Clear();
    }

    public void Clear()
    {
        _isDead = true;
        rb.velocity = Vector3.zero;
        EnemyPool.Instance?.Return(gameObject);
        if (_spawnPool != null && !string.IsNullOrEmpty(_enemyId))
        {
            _spawnPool.DecrementAlive(_enemyId, this);
        }
    }

    // ===== 重置方法（供对象池使用）=====
    public void ResetEnemy()
    {
        _isActive = false;
        _isDead = false;
        _lastShootTime = 0f;

        if (_shootingRoutine != null)
        {
            StopCoroutine(_shootingRoutine);
            _shootingRoutine = null;
        }

        OnReady = null; // 防止事件残留
    }

    // 实现 IDamageable 接口
    public float GetDefense() => _runtimeStats.defense;
    public float GetDamageReduction() => _runtimeStats.damageReduction;
}