using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualFeedback : MonoBehaviour
{
    [Header("受伤时替换的子物体精灵")]
    [Tooltip("要更换精灵的子物体（必须带 SpriteRenderer）")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Tooltip("正常状态下的默认精灵")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("受伤时显示的精灵（例如红色闪烁、裂纹等）")]
    [SerializeField] private Sprite damagedSprite;

    [Tooltip("受伤后精灵保持的时间（秒）")]
    [SerializeField] private float flashDuration = 0.1f;

    private void OnEnable()
    {
        // 订阅 Health 的 OnDamaged 事件
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDamaged.AddListener(OnPlayerDamaged);
        }
        else
        {
            Debug.LogWarning("PlayerVisualFeedback: 未找到 Health 组件，无法监听受伤事件！", this);
        }
    }

    private void OnDisable()
    {
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDamaged.RemoveListener(OnPlayerDamaged);
        }
    }

    private void OnPlayerDamaged(int currentHealth)
    {
        if (currentHealth <= 0) return; // 死亡时不处理（可选）

        StartCoroutine(FlashDamagedSprite());
    }

    private IEnumerator FlashDamagedSprite()
    {
        // 切换为受伤精灵
        if (targetSpriteRenderer != null && damagedSprite != null)
        {
            targetSpriteRenderer.sprite = damagedSprite;
        }

        yield return new WaitForSeconds(flashDuration);

        // 恢复默认精灵
        if (targetSpriteRenderer != null && defaultSprite != null)
        {
            targetSpriteRenderer.sprite = defaultSprite;
        }
    }
}
