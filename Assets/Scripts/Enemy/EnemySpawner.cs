using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")] 
    public float waveDuration = 60f;           // 一波持续时间（秒）
    [HideInInspector] public float spawnInterval = 1f; // 运行时由 GrowthCurve 覆盖
    public bool autoSpawn = false;
    public bool loop = false;
    
    [Header("生成数量")]
    [HideInInspector] public int spawnNum = 3; // 运行时由 GrowthCurve 覆盖

    [Header("生成位置")]
    public BoxCollider spawnAreaCollider;
    public GameObject warningAreaPrefab;
    public Vector3 spawnCenter = Vector3.zero;
    public bool useFixedSpawnCenter = false;
    public float spawnRadius = 3f;

    [Header("依赖")]
    public SpawnPool spawnPool;

    private int _currentWave = 1;
    private EnemyGrowthCurve _growthCurve;

    // 内部状态
    private ObjectPool<SpawnWarningIndicator> _warningPool;
    private readonly System.Random _customRandom = new System.Random(LevelGenerator.seed);
    private bool isSpawning = false;
    private Coroutine _spawnRoutine;

    public UnityEvent<Vector3> OnEnemySpawnPositionReported = new Vector3UnityEvent();
    public event Action OnSpawnerCompleted;

    void Awake()
    {
        if (warningAreaPrefab != null)
        {
            _warningPool = new ObjectPool<SpawnWarningIndicator>(
                createFunc: () => Instantiate(warningAreaPrefab).GetComponent<SpawnWarningIndicator>(),
                actionOnGet: indicator => indicator.gameObject.SetActive(true),
                actionOnRelease: indicator => {
                    indicator.Reset();
                    indicator.gameObject.SetActive(false);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 30
            );
        }
    }

    void Start()
    {
        if (autoSpawn)
        {
            SpawnWave();
        }
    }

    public void SetCurrentWaveAndGrowth(int wave, EnemyGrowthCurve curve)
    {
        _currentWave = wave;
        _growthCurve = curve;
    }

    public void SpawnWave()
    {
        if (isSpawning || spawnPool == null || _warningPool == null) return;

        // ✅ 关键：从 GrowthCurve 获取当前波次参数
        if (_growthCurve != null)
        {
            spawnNum = _growthCurve.GetBatchSize(_currentWave);
            spawnInterval = _growthCurve.GetSpawnInterval(_currentWave);
            Debug.Log($"[Wave {_currentWave}] Spawning {spawnNum} enemies every {spawnInterval:F2}s");
        }
        else
        {
            Debug.LogWarning("No growth curve assigned! Using default values.");
        }

        _spawnRoutine = StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        float waveEndTime = Time.time + waveDuration;

        while (Time.time < waveEndTime && gameObject.activeInHierarchy)
        {
            SpawnSomeEnemyWithWarning();
            yield return new WaitForSeconds(spawnInterval);
        }

        if (spawnPool != null && spawnPool.TotalAlive > 0)
        {
            Debug.Log("Wave time ended. Killing remaining enemies.");
            spawnPool.KillAllEnemies();
            yield return null;
        }

        if (loop && gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(SpawnWaveRoutine());
        }
        else
        {
            isSpawning = false;
            OnSpawnerCompleted?.Invoke();
            Debug.Log("Spawner completed.");
        }
    }

    void SpawnSomeEnemyWithWarning()
    {
        Vector3[] spawnPositions = GetSomeSpawnPositions();

        for (int i = 0; i < spawnNum && i < spawnPositions.Length; i++)
        {
            int index = i;
            EnemyData selected = spawnPool.SelectRandom();
            if (selected == null)
            {
                Debug.LogWarning("Cannot spawn enemy: all types at maxAlive limit.");
                continue;
            }

            SpawnWarningIndicator indicator = _warningPool.Get();
            indicator.transform.position = spawnPositions[index] + Vector3.up * 0.01f;

            void OnFinish()
            {
                indicator.OnWarningFinished -= OnFinish;
                _warningPool.Release(indicator);

                GameObject enemy = EnemyPool.Instance?.Get(selected.id);
                if (enemy != null)
                {
                    Vector3 spawnPos = spawnPositions[index];
                    enemy.transform.position = spawnPos;
                    OnEnemySpawnPositionReported?.Invoke(spawnPos);
                    var controller = enemy.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        controller.OnSpawned(selected.id, spawnPool, selected);
                    }
                }
            }

            indicator.OnWarningFinished += OnFinish;
            indicator.StartWarning();
        }
    }

    private Vector3[] GetSomeSpawnPositions()
    {
        Vector3 center = useFixedSpawnCenter ? spawnCenter : GetRandomPositionInArea();
        Vector3[] positions = new Vector3[spawnNum];
        for (int i = 0; i < spawnNum; i++)
        {
            double angle = _customRandom.NextDouble() * 2 * Math.PI;
            double distance = Math.Sqrt(_customRandom.NextDouble()) * spawnRadius;
            Vector3 offset = new Vector3(
                (float)(Math.Cos(angle) * distance),
                0,
                (float)(Math.Sin(angle) * distance)
            );

            Vector3 candidate = center + offset;

            if (spawnAreaCollider != null)
            {
                Bounds bounds = spawnAreaCollider.bounds;
                candidate.x = Mathf.Clamp(candidate.x, bounds.min.x, bounds.max.x);
                candidate.z = Mathf.Clamp(candidate.z, bounds.min.z, bounds.max.z);
            }

            positions[i] = candidate;
        }
        return positions;
    }

    public Vector3 GetRandomPositionInArea()
    {
        if (spawnAreaCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = spawnAreaCollider.bounds;
        double x = bounds.min.x + bounds.size.x * _customRandom.NextDouble();
        double z = bounds.min.z + bounds.size.z * _customRandom.NextDouble();
        return new Vector3((float)x, transform.position.y, (float)z);
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
            isSpawning = false;
        }
    }

    void OnDisable() => StopSpawning();
    void OnDestroy() => StopSpawning();
}

