using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance{get; private set;}

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            
            Destroy(gameObject);
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }
    [Header("")]
    public Transform player;
    
    [Header("Game Stats")]
    public bool isGameOver = false;

    public int currentWace = 1;
    
    public float gameTime = 0;
    
    [Header("Resources")]
    private int _score = 0;
    private int _experience = 0;
    public UnityEvent<int> onScoreChanged;
    public UnityEvent<int> onExperienceChanged;
    public UnityEvent onLevelUp;
    [Header("config")]
    public int experiencePerLevel = 50;
    private int _currentLevel = 1;
    [Header("Game Events")]
    public UnityEvent onGameOver;      // 游戏失败
    public UnityEvent onGameWin;       // 游戏胜利
    public UnityEvent onGamePause;     // 👈 暂停开始
    public UnityEvent onGameResume;    // 👈 新增：恢复游戏（推荐添加）
    private bool _isPaused = false;
    void Start()
    {
        ResetGame();
        
    }

    void Update()
    {
        
    }
    public void ResetGame()
    {
        isGameOver = false;
        currentWace = 1;
        gameTime = 0f;
        _score = 0;
        _experience = 0;
        _currentLevel = 1;
        _isPaused = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // 恢复默认物理步长
    }
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// 暂停游戏（时间冻结 + 事件广播）
    /// </summary>
    public void PauseGame()
    {
        if (_isPaused || isGameOver) return;

        _isPaused = true;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f; // 防止 FixedUpdate 积累

        onGamePause?.Invoke(); // 👈 广播暂停事件（UI 可监听）
    }

    /// <summary>
    /// 恢复游戏（时间恢复 + 事件广播）
    /// </summary>
    public void ResumeGame()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // 恢复物理更新

        onGameResume?.Invoke(); // 👈 广播恢复事件
    }

    public bool IsPaused() => _isPaused;

    // ===== 游戏结束逻辑 =====
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        onGameOver?.Invoke();
    }

    public void TriggerGameWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        onGameWin?.Invoke();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }
    
    public void AddScore(int value){}
    public void AddExperience(int value){}
}
