using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class WaveSpawnerData
{
    public EnemySpawner spawnerPrefab; // 预制的 Spawner（含配置）
    public Vector3 positionOffset = Vector3.zero;
    public bool overrideAutoSpawn = true; // 强制由 WaveManager 控制
}

[Serializable]
public class WaveData
{
    public string waveName = "Wave 1";
    public float preWaveDelay = 3f;      // 波次开始前等待（如“第3波即将开始！”）
    public float postWaveDelay = 2f;     // 本波结束后等待
    public bool isBossWave = false;

    [Tooltip("本波要激活的 Spawner 列表")]
    public WaveSpawnerData[] spawners;
}

