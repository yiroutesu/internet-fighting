using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LevelGenerator : MonoBehaviour
{
    public static int seed=(seed!=0?seed:System.DateTime.Now.Millisecond);//种子，用作全局的随机生成
    public EnemySpawner enemySpawner;//敌怪的生成系统
    Coroutine c1=null;
    private void Start()
    {
    }
    private void Update()
    {
        
        
    }
}
