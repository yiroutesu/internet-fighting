using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//敌怪对象池
public class EnemyPool:MonoBehaviour
{
  [SerializeField]
  public SpawnPool spawnPool;
  
  public int initialSize=5;
   [SerializeField]
   private Dictionary<string, Queue<GameObject>> poolDict = new();
   //单例
   public static EnemyPool Instance { get; private set; }
   void Awake()
   {
      if (Instance != null&&Instance != this)
      {
         Destroy(gameObject);
      }
      Instance=this;
      InitializePool();
   }

   private void InitializePool()
   {
      if (spawnPool == null)
      {
         Debug.LogError("EnemyPool: SpawnPool not assigned!");
         return;
      }
      var enemies = spawnPool.GetRegisteredEnemies();
      foreach (var data in enemies)
      {
         Queue<GameObject> queue = new Queue<GameObject>();
         for (int i = 0; i <initialSize; i++)
         {
            var newEnemy= Instantiate(data.prefab);
            newEnemy.SetActive(false);
            queue.Enqueue(newEnemy);
         }
         poolDict.Add(data.id, queue);
      }
   }
   public GameObject Get(string key)
   {
      if(poolDict.TryGetValue(key, out var pool)&&pool.Count > 0)
      {
         GameObject obj=pool.Dequeue();
         obj.SetActive(true);
         return obj;
      }
      var enemies = spawnPool.GetRegisteredEnemies();
      var item = enemies.Find(e => e.id == key);
      if (item != null)
      {
         var newObj = Instantiate(item.prefab, transform);
         newObj.SetActive(true);
         return newObj;
      }
      
      return null;
   }

   public void Return(GameObject obj)
   {
      var identifier= obj.GetComponent<EnemyIdentifier>();
      if (identifier == null)
      {
         Destroy(obj);
         Debug.Log("The Obj is Destroyed");
      }
      string key = identifier.enemyKey;
      if (poolDict.ContainsKey(key))
      {
         obj.SetActive(false);
         obj.transform.SetParent(transform);
         poolDict[key].Enqueue(obj);
      }
      else
      {
         Debug.LogWarning($"Return a GameObject with unknown key：{key}");
      }
   }
}
