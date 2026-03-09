using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    
    //单例
    public static EconomyManager Instance;

    [SerializeField]
    private int _gold = 0;

    //此变量用来给外部类显示金币数量
    //EconomyManager.Instance.Gold;
    public int Gold => _gold;

    //用作广播金币变化，调用该事件会返回一个int值
    //注册方法：EconomyManager.Instance.OnGoldChanged += f(x);
    //f(x)代表金币数量改变时需要调用的函数，可用于UI更新函数；
    public event Action<int> OnGoldChanged;

    //此函数用于金币的添加
    public void AddGold(int amount)
    {
        _gold += amount;
        //通知所有的监听者金币数量已经改变了
        OnGoldChanged?.Invoke(_gold);
    }

    //此函数用于检测金币是否大于花费
    //是就扣费，返回true
    //否就不操作，返回false
    public bool TryRemoveGold(int cost)
    {
        if (_gold >= cost)
        {
            _gold -= cost;
            OnGoldChanged?.Invoke(_gold);
            return true;

        }

        return false;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }

        Instance = this;

    }
}
    
