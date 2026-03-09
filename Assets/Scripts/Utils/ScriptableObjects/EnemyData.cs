using UnityEngine;

/// <summary>
/// 敌人数据资产 —— 在 Project 中右键创建 .asset 文件
/// </summary>
[CreateAssetMenu(
    fileName = "New Enemy",
    menuName = "Game Data/Enemies/Enemy Data",
    order = 1)]
public class EnemyData : ScriptableObject
{
    // 嵌入 EnemyStats，便于在 Inspector 中编辑
    public string id;
    public GameObject prefab;
    public EnemyStats stats;
}