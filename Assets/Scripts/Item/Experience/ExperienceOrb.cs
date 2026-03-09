// ExperienceOrb.cs
using UnityEditor.UIElements;
using UnityEngine;
using System.Collections;

public class ExperienceOrb : MonoBehaviour
{
    [Header("经验值设置")]
    public int experienceValue = 10;

    [Header("吸引设置")]
    [Tooltip("经验球飞向玩家的速度")]
    public float attractionSpeed = 5f; // 注意：单位是 m/s，不是 50！

    [Header("生命周期")]
    [Tooltip("若未被拾取，多少秒后自动消失")]
    public float lifetime = 60f;

    [Header("碰撞层设置")]
    [Tooltip("进入此 Layer 的物体会触发吸引（通常是玩家的大范围 Trigger）")]
    public LayerMask attractionLayer;

    [Tooltip("进入此 Layer 的物体会触发拾取（通常是玩家的小范围 Trigger）")]
    public LayerMask pickupLayer;

    private Transform _player;
    private bool _isCollected = false;
    private bool _isAttracted = false;
    private Coroutine _despawnCoroutine;

    // 外部回调：用于通知 Spawner 回收到对象池
    public System.Action onCollected;

    /// <summary>
    /// 由 ExperienceOrbSpawner 调用，重置状态并激活
    /// </summary>
    public void ResetOrb(int newExperienceValue, float newLifetime = 60f)
    {
        experienceValue = newExperienceValue;
        _isCollected = false;
        _isAttracted = false;

        // 清理旧协程
        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }

        // 重新启用 Collider（SetActive(true) 通常会启用，但显式更安全）
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        // 获取玩家引用（每次重置都更新，避免玩家切换）
        _player = GameManager.Instance?.player;

        // 启动超时销毁
        _despawnCoroutine = StartCoroutine(AutoDespawnAfter(newLifetime));
    }

    private IEnumerator AutoDespawnAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!_isCollected)
        {
            Collect(); // 自动回收（即使没被玩家捡）
        }
    }

    void FixedUpdate()
    {
        if (_isCollected || _player == null || !_isAttracted) return;

        // 平滑飞向玩家（使用 Time.fixedDeltaTime 保证物理一致性）
        Vector3 direction = (_player.position - transform.position).normalized;
        transform.position += direction * attractionSpeed * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCollected || other == null) return;

        int layer = other.gameObject.layer;

        // 检查是否进入吸引范围
        if ((attractionLayer.value & (1 << layer)) != 0)
        {
            _isAttracted = true;
        }

        // 检查是否进入拾取范围
        if ((pickupLayer.value & (1 << layer)) != 0)
        {
            Collect();
        }
    }

    /// <summary>
    /// 拾取经验球（可被玩家触发，也可被超时触发）
    /// </summary>
    public void Collect()
    {
        if (_isCollected) return;
        _isCollected = true;

        // 立即禁用 Collider，防止多次触发
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 停止超时协程
        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }

        // 通知游戏系统
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddExperience(experienceValue);
        }
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(experienceValue); // 可选：金币=经验（或按比例）
        }

        // 通知 Spawner 回收（关键！）
        onCollected?.Invoke();
        AudioManager.instance?.Play("coin_collect");

        // 隐藏对象（供对象池复用）
        gameObject.SetActive(false);
    }
}