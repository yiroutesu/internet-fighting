// Assets/Scripts/Player/StatSystem.cs
using System.Collections.Generic;
using UnityEngine;
using SIGame.Enums;
using SIGame.Stats;
using UnityEngine.Events;

public class StatSystem : MonoBehaviour, IStatSystem
{
    #region 单例
    private static StatSystem _instance;
    public static StatSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // 场景里找不到就当场创建一个
                _instance = FindObjectOfType<StatSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(StatSystem));
                    _instance = go.AddComponent<StatSystem>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // 保证全局唯一
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // 如果希望跨场景存在，取消下面一行注释
        // DontDestroyOnLoad(gameObject);

        InitStats();
    }
    #endregion

    #region 原有逻辑
    [System.Serializable]
    public class Stat
    {
        public float baseValue = 0f;
        public float flatBonus = 0f;
        public float percentBonus = 0f;

        public float FinalValue =>
            (baseValue + flatBonus) * (1f + percentBonus / 100f);

        public void AddFlat(float value) => flatBonus += value;
        public void RemoveFlat(float value) => flatBonus -= value;
        public void AddPercent(float value) => percentBonus += value;
        public void RemovePercent(float value) => percentBonus -= value;
        public void ResetModifiers()
        {
            flatBonus = 0f;
            percentBonus = 0f;
        }
    }

    [Header("属性配置")]
    public PlayerStatsSO playerStatsSO;

    private Dictionary<PlayerStatAttr, Stat> stats = new();

    public UnityEvent<PlayerStatAttr, float> OnStatChanged;

    private void InitStats()
    {
        foreach (PlayerStatAttr attr in System.Enum.GetValues(typeof(PlayerStatAttr)))
            stats[attr] = new Stat();

        if (playerStatsSO != null)
        {
            foreach (PlayerStatAttr attr in System.Enum.GetValues(typeof(PlayerStatAttr)))
                stats[attr].baseValue = playerStatsSO.GetBaseValue(attr);
        }
        else
        {
            Debug.LogWarning("StatSystem: playerStatsSO 未设置，使用默认值！", this);
            stats[PlayerStatAttr.MaxHP].baseValue = 100f;
            stats[PlayerStatAttr.AttackDamage].baseValue = 100f;
            stats[PlayerStatAttr.AttackSpeed].baseValue = 1f;
            stats[PlayerStatAttr.MoveSpeed].baseValue = 30f;
            stats[PlayerStatAttr.CritChance].baseValue = 0f;
            stats[PlayerStatAttr.CritMultiplier].baseValue = 1.5f;
            stats[PlayerStatAttr.Armor].baseValue = 0f;
            stats[PlayerStatAttr.XPpickRange].baseValue = 3f;
        }
    }

    public void AddFlatModifier(PlayerStatAttr stat, float value)
    {
        if (stats.TryGetValue(stat, out var s)) s.AddFlat(value);
        OnStatChanged?.Invoke(stat, s.FinalValue);
    }

    public void RemoveFlatModifier(PlayerStatAttr stat, float value)
    {
        if (stats.TryGetValue(stat, out var s)) s.RemoveFlat(value);
        OnStatChanged?.Invoke(stat, s.FinalValue);
    }

    public void AddPercentModifier(PlayerStatAttr stat, float value)
    {
        if (stats.TryGetValue(stat, out var s)) s.AddPercent(value);
        OnStatChanged?.Invoke(stat, s.FinalValue); 
    }

    public void RemovePercentModifier(PlayerStatAttr stat, float value)
    {
        if (stats.TryGetValue(stat, out var s)) s.RemovePercent(value);
        OnStatChanged?.Invoke(stat, s.FinalValue);
    }

    public float GetFinalValue(PlayerStatAttr stat) =>
        stats.TryGetValue(stat, out var s) ? s.FinalValue : 0f;

    public float GetValue(PlayerStatAttr stat) => GetFinalValue(stat);

    public void ResetAllModifiers()
    {
        foreach (var stat in stats.Values) stat.ResetModifiers();
    }
    #endregion
}