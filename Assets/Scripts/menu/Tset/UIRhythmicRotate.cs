// UIRhythmicRotate.cs
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIRhythmicRotate : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateDuration = 0.3f;       // 旋转动画时长（秒）
    public float pauseDuration = 0.1f;        // 停顿时长（秒）

    [Header("Control")]
    public bool autoStart = true;             // 启动时自动开始
    public bool useOvershoot = true;          // 是否启用回弹增强节奏感

    private RectTransform rectTransform;
    private Coroutine rotationRoutine;
    private bool isRunning = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (autoStart)
            StartRotating();
    }

    public void StartRotating()
    {
        if (isRunning) return;
        rotationRoutine = StartCoroutine(RotationLoop());
        isRunning = true;
    }

    public void StopRotating()
    {
        if (rotationRoutine != null)
        {
            StopCoroutine(rotationRoutine);
            rotationRoutine = null;
            isRunning = false;
        }
    }

    System.Collections.IEnumerator RotationLoop()
    {
        while (true)
        {
            yield return Rotate90WithEase();
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    System.Collections.IEnumerator Rotate90WithEase()
    {
        Quaternion startRot = rectTransform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0, 0, -90); // -90 = 顺时针（UI Y 轴朝外）

        float elapsed = 0f;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用 unscaledTime，避免 Time.timeScale=0 时卡住
            float t = Mathf.Clamp01(elapsed / rotateDuration);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (useOvershoot)
            {
                // Overshoot: 超过目标再弹回
                easedT = 1.1f * easedT - 0.1f * easedT * easedT;
            }

            rectTransform.localRotation = Quaternion.Slerp(startRot, targetRot, easedT);
            yield return null;
        }

        rectTransform.localRotation = targetRot;
    }
}