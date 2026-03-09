using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Utils/DamageSO")]   // 在右键菜单里能直接新建
public class DamageSO : ScriptableObject
{
    [Header("伤害")]
    public int damage = 10;

    [Header("命中效果")]
    public GameObject onHitFx;          // 粒子预制体
    public AudioClip  onHitSfx;         // 音效
    public bool destroyAfterHit = true;
    public LayerMask targetLayers;
}
