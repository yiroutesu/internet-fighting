// MainMenuManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Loading UI (Optional but recommended)")]
    public GameObject loadingOverlay;          // 全屏遮罩（含旋转正方形）
    public UIRhythmicRotate rotatingSquare;    // 旋转动画组件（来自之前的脚本）

    [Header("Scene Settings")]
    public string mainGameSceneName = "Game";  // 推荐用名字而非索引（更安全）
    // 或保留用 buildIndex：
    // public int mainGameBuildIndex = 1;

    public void StartGame()
    {
        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        // 显示加载界面（如果已设置）
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            rotatingSquare?.StartRotating(); // 安全调用
        }

        // 异步加载场景（推荐用名字，避免 Build Settings 顺序变动出错）
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainGameSceneName);
        // AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainGameBuildIndex); // 如果坚持用索引

        // 关键：禁止自动激活，以便控制跳转时机
        asyncLoad.allowSceneActivation = false;

        // 等待加载完成（Unity 中 progress 最大到 0.9）
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 可选：确保加载界面至少显示一小段时间（防闪屏）
        yield return new WaitForSecondsRealtime(0.5f);

        // 激活场景 → 自动切换
        asyncLoad.allowSceneActivation = true;

        // 注意：loadingOverlay 会在场景切换后自动销毁（无需手动隐藏）
    }
}