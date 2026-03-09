using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//此脚本预想作为敌怪的对象
//Enemy作为基类，由此在继承子类
public class Enemy : MonoBehaviour
{
    public int health;
    public int damage;
}

public class MeleeEnemy : Enemy
{
    
}

public class RangedEnemy : Enemy
{
    
}
