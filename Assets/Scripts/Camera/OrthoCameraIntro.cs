// OrthoCameraIntro.cs
using UnityEngine;
using Cinemachine;
using System;

public class OrthoCameraIntro : MonoBehaviour
{
    public static OrthoCameraIntro Instance { get; private set; }

    [Header("Cinemachine")]
    public CinemachineVirtualCamera vcam;

    [Header("Zoom Settings")]
    [Tooltip("战斗/展示全场时的 orthographic size（大值）")]
    public float combatViewSize = 15f;

    [Tooltip("聚焦玩家时的 orthographic size（小值）")]
    public float focusViewSize = 5f;

    [Range(0.1f, 5f)]
    public float transitionDuration = 1.5f;

    public bool useSmoothStep = true;

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 初始状态：聚焦玩家（小视野）
        if (vcam != null)
        {
            vcam.m_Lens.OrthographicSize = focusViewSize;
        }
        else
        {
            Debug.LogError("OrthoCameraIntro: vcam is not assigned!");
        }
    }

    /// <summary>
    /// 回合开始前调用：从小视野拉远到大视野，完成后触发回调（用于开始刷怪）
    /// </summary>
    public void BeginRoundTransition(Action onTransitionComplete = null)
    {
        if (isTransitioning || vcam == null) return;
        StartCoroutine(Zoom(focusViewSize, combatViewSize, onTransitionComplete));
    }

    /// <summary>
    /// 回合结束后调用：从大视野推近回小视野，完成后可选回调
    /// </summary>
    public void EndRoundTransition(Action onTransitionComplete = null)
    {
        if (isTransitioning || vcam == null) return;
        StartCoroutine(Zoom(combatViewSize, focusViewSize, onTransitionComplete));
    }

    System.Collections.IEnumerator Zoom(float from, float to, Action onComplete)
    {
        isTransitioning = true;
        vcam.m_Lens.OrthographicSize = from;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = useSmoothStep 
                ? Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration) 
                : elapsed / transitionDuration;
            
            vcam.m_Lens.OrthographicSize = Mathf.Lerp(from, to, t);
            yield return null;
        }

        vcam.m_Lens.OrthographicSize = to;
        isTransitioning = false;
        onComplete?.Invoke();
    }
}