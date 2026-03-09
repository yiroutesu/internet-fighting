using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UGUIManger : MonoBehaviour
{
    private RectTransform rectTransform; // 要设置位置的 RectTransform
    private Vector2 bagUIposition;
    private Vector2 shopUIposition;
    [SerializeField] private float duration;//UI移动平滑时间
    [SerializeField] private Button moveToBag;
    [SerializeField] private Button moveToShop;
    [SerializeField] private TMP_Text XP;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        bagUIposition = new Vector2(0, 0);
        float screenWidth = Screen.width;
        shopUIposition = new Vector2(-screenWidth * 0.55f, 0);
        moveToShop.gameObject.SetActive(false);
        Move(shopUIposition);
        moveToBag.onClick.AddListener(MoveToBag);
        moveToShop.onClick.AddListener(MoveToShop);
        EconomyManager.Instance.OnGoldChanged+=OnGoldChanged;
    }

    private void OnGoldChanged(int obj)
    {
        XP.text =obj.ToString();
    }

    private void MoveToShop()
    {
        moveToBag.gameObject.SetActive(true);
        moveToShop.gameObject.SetActive(false);
        StartCoroutine(MoveToPosition(shopUIposition));


    }

    private void MoveToBag()
    {
        moveToShop.gameObject.SetActive(true);
        moveToBag.gameObject.SetActive(false);
        StartCoroutine(MoveToPosition(bagUIposition));
    }
    private IEnumerator MoveToPosition(Vector2 target)
    {
        float elapsedTime = 0;
        Vector2 startPosition = rectTransform.anchoredPosition;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, target, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = target; // 确保最终位置精确到达目标位置
    }

    private void Move(Vector2 target)
    {
        rectTransform.anchoredPosition = target;
    }
}