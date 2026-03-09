using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance;

    private CinemachineImpulseSource impulseSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        impulseSource = GetComponent<CinemachineImpulseSource>();
        
    }

    // 外部调用此方法触发震动
    public void Shake()
    {
        if(impulseSource == null)
            Debug.LogWarning("没找到震动组件");
        else
        {
            Debug.LogWarning("找到了震动组件");
        }
        impulseSource?.GenerateImpulse();
    }
}
