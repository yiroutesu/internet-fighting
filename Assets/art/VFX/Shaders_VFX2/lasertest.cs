using UnityEngine;
using System.Collections;

public class LaserTest : MonoBehaviour
{
    [Header("激光引用")]
    public LaserController[] lasers;
    
    [Header("演示设置")]
    public float sequenceDelay = 2f;
    public Color[] colorSequence;
    
    void Start()
    {
        if (lasers.Length == 0)
            lasers = FindObjectsOfType<LaserController>();
        
        StartCoroutine(DemoSequence());
    }
    
    IEnumerator DemoSequence()
    {
        while (true)
        {
            // 1. 激活所有激光
            foreach (var laser in lasers)
            {
                laser.SetActive(true);
                laser.SetRandomNoise();
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(sequenceDelay);
            
            // 2. 颜色循环
            foreach (Color color in colorSequence)
            {
                foreach (var laser in lasers)
                {
                    laser.SetColor(color, Color.white);
                }
                yield return new WaitForSeconds(1f);
            }
            
            // 3. 冲击波效果
            foreach (var laser in lasers)
            {
                laser.TriggerShockwave(3f, 0.3f);
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(sequenceDelay);
            
            // 4. 脉动效果
            for (int i = 0; i < 5; i++)
            {
                foreach (var laser in lasers)
                {
                    laser.pulseSpeed = 2f + i * 0.5f;
                }
                yield return new WaitForSeconds(0.5f);
            }
            
            // 5. 关闭激光
            foreach (var laser in lasers)
            {
                laser.SetActive(false);
                yield return new WaitForSeconds(0.1f);
            }
            
            yield return new WaitForSeconds(sequenceDelay);
        }
    }
    
    void Update()
    {
        // 键盘控制
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var laser in lasers)
            {
                laser.SetActive(!laser.enabled);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var laser in lasers)
            {
                laser.SetColor(Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f));
            }
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            foreach (var laser in lasers)
            {
                laser.TriggerShockwave(Random.Range(2f, 5f));
            }
        }
    }
}