// WaveManager.cs
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("波次设置")]
    public int currentWave = 0;
    public float firstWaveDelay = 2f;

    [Header("依赖")]
    public EnemySpawner enemySpawner;
    public EnemyGrowthCurve growthCurve;
    
    [Header("Boss 设置")]
    public GameObject bossPrefab;
    public List<int> bossWaves = new() { 5, 10, 15 };

    private BossContext currentBoss;
    private bool isWaveInProgress = false;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartNextWave()
    {
        if (isWaveInProgress)
        {
            Debug.LogWarning("Wave already in progress. Ignoring extra call.");
            return;
        }

        if (enemySpawner == null)
        {
            Debug.LogError("WaveManager: EnemySpawner is not assigned!");
            return;
        }

        currentWave++;
        isWaveInProgress = true;

        if (currentWave == 1)
        {
            // 第一波：先等全局延迟，再拉远镜头，再刷怪
            StartCoroutine(DelayThenStartWave(firstWaveDelay));
        }
        else
        {
            // 后续波次：直接请求镜头拉远，完成后刷怪
            OrthoCameraIntro.Instance?.BeginRoundTransition(() =>
            {
                StartWaveInternal(currentWave);
            });
        }
    }

    IEnumerator DelayThenStartWave(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        OrthoCameraIntro.Instance?.BeginRoundTransition(() =>
        {
            StartWaveInternal(currentWave);
        });
    }

    private void StartWaveInternal(int wave)
    {
        OnWaveStarted?.Invoke(wave);

        if (IsBossWave(wave))
        {
            SpawnBossWave();
        }
        else
        {
            enemySpawner.SetCurrentWaveAndGrowth(wave, growthCurve);
            enemySpawner.OnSpawnerCompleted -= OnCurrentWaveFinished;
            enemySpawner.OnSpawnerCompleted += OnCurrentWaveFinished;
            enemySpawner.SpawnWave();
        }
    }

    void SpawnBossWave()
    {
        if (bossPrefab == null)
        {
            Debug.LogError("Boss prefab is not assigned!");
            OnCurrentWaveFinished();
            return;
        }

        Vector3 spawnPos = enemySpawner.GetRandomPositionInArea();
        Vector3 DeltaY=new Vector3(0,4.5f,0);
        GameObject bossObj = Instantiate(bossPrefab, spawnPos+DeltaY, Quaternion.identity);

        currentBoss = bossObj.GetComponent<BossContext>();
        if (currentBoss == null)
        {
            Debug.LogError("Boss prefab missing SimpleBossAI!");
            OnCurrentWaveFinished();
            return;
        }

        currentBoss.OnBossDied.AddListener(OnBossDefeated);
    }

    void OnBossDefeated()
    {
        if (currentBoss != null)
        {
            currentBoss.OnBossDied.RemoveListener(OnBossDefeated);
            currentBoss = null;
        }
        StartCoroutine(DelayedWaveFinish(5f));
    }

    IEnumerator DelayedWaveFinish(float delaySeconds)
    {
        Debug.Log($"Boss defeated! Waiting {delaySeconds} seconds before ending wave...");
        yield return new WaitForSecondsRealtime(delaySeconds);
        if (this == null) yield break;
        OnCurrentWaveFinished();
    }

    bool IsBossWave(int wave) => bossWaves.Contains(wave);

    void OnCurrentWaveFinished()
    {
        isWaveInProgress = false;
        enemySpawner.OnSpawnerCompleted -= OnCurrentWaveFinished;
        OnWaveCompleted?.Invoke(currentWave);

        // 🔥 关键：回合结束，镜头推近回玩家
        OrthoCameraIntro.Instance?.EndRoundTransition();

        Debug.Log($"✅ Wave {currentWave} completed.");
    }

    void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnSpawnerCompleted -= OnCurrentWaveFinished;
        }
        if (currentBoss != null)
        {
            currentBoss.OnBossDied.RemoveListener(OnBossDefeated);
        }
    }

    // 可由 UI 或 GameManager 调用
    public void RoundStart() => StartNextWave();

    public void ClearWave()
    {
        StopAllCoroutines();
        enemySpawner?.StopAllCoroutines();
        enemySpawner?.spawnPool?.KillAllEnemies();
    }
}