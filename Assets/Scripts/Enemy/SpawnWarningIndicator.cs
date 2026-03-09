using System;
using UnityEngine;
using System.Collections;
public class SpawnWarningIndicator : MonoBehaviour
{
    public float warningDuration = 1f;

    public Action OnWarningFinished; // 事件回调

    private Coroutine _routine;

    public void StartWarning()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(WarningRoutine());
    }

    IEnumerator WarningRoutine()
    {
        // 可选：添加视觉效果（如缩放、闪烁）
        // 例如：让红圈从 0 缩放到 1
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * 1.5f; // 根据你的美术调整

        while (elapsed < warningDuration)
        {
            float t = elapsed / warningDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终尺寸
        transform.localScale = targetScale;

        // 触发完成事件
        OnWarningFinished?.Invoke();
    }

    public void Reset()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        OnWarningFinished = null; // 👈 唯一清理事件的地方
    }
    // 供外部强制停止（可选）
    public void Cancel()
    {
        Reset();
    }
}