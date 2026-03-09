using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGame.Enums;
//武器基类
public abstract class WeaponBehavior : MonoBehaviour
{
    protected GameObject owner;
    protected StatSystem statSystem;
    protected float lastAttackTime;
    
    public virtual void Initialize(GameObject ownerObj)
    {
        this.owner = ownerObj;
        this.statSystem = owner.GetComponent<StatSystem>();
        if (statSystem == null)
            Debug.LogError("Weapon owner missing StatSystem!", this);

        StartAttackRoutine();
    }

    protected virtual void StartAttackRoutine()
    {
        InvokeRepeating(nameof(PerformAttack), 0f, GetAttackInterval());
    }

    protected virtual float GetAttackInterval()
    {
        float attackSpeed = statSystem.GetFinalValue(PlayerStatAttr.AttackSpeed);
        // AttackSpeed 越高，间隔越短。假设 1 = 每秒1次
        return attackSpeed > 0 ? 1f / attackSpeed : 1f;
    }

    protected abstract void PerformAttack();

    protected virtual void OnDestroy()
    {
        CancelInvoke(nameof(PerformAttack));
    }
}