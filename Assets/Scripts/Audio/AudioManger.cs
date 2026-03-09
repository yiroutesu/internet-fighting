using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    
    [Header("音频设置")]
    [SerializeField] private string audioResourcesPath = "Audio";
    [SerializeField] private int maxPoolSize = 20; // AudioSource 池的最大容量
    [SerializeField] private int prewarmCount = 5; // 预创建的 AudioSource 数量
    
    // 存储所有 AudioType SO
    private List<AudioType> audioTypes = new List<AudioType>();
    
    // 音频缓存字典
    private Dictionary<string, AudioType> audioDictionary = new Dictionary<string, AudioType>();
    
    // AudioSource 对象池
    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    
    // 用于存储允许叠加播放的音频实例
    private Dictionary<string, List<AudioSource>> audioInstances = new Dictionary<string, List<AudioSource>>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeAudioSourcePool();
            LoadAudioTypesFromResources();
            InitializeAudioDictionary();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // 初始化 AudioSource 对象池
    private void InitializeAudioSourcePool()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            CreateAndPoolAudioSource();
        }
        Debug.Log($"[AudioManager] 音频对象池已初始化，预创建 {prewarmCount} 个 AudioSource");
    }
    
    // 创建并池化 AudioSource
    private AudioSource CreateAndPoolAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.enabled = false; // 禁用组件，减少开销
        audioSourcePool.Enqueue(newSource);
        return newSource;
    }
    
    // 从池中获取 AudioSource
    private AudioSource GetAudioSourceFromPool()
    {
        // 如果池为空且未达到最大限制，创建新的 AudioSource
        if (audioSourcePool.Count == 0 && activeAudioSources.Count < maxPoolSize)
        {
            CreateAndPoolAudioSource();
        }
        
        // 从池中取出
        if (audioSourcePool.Count > 0)
        {
            AudioSource source = audioSourcePool.Dequeue();
            source.enabled = true;
            activeAudioSources.Add(source);
            return source;
        }
        
        // 池为空且达到最大限制，复用最旧的非循环音频
        if (activeAudioSources.Count > 0)
        {
            // 寻找非循环且未播放的 AudioSource
            AudioSource oldestSource = activeAudioSources
                .Where(s => !s.loop && !s.isPlaying)
                .FirstOrDefault();
            
            if (oldestSource != null)
            {
                // 清理旧的音频设置
                ClearAudioSource(oldestSource);
                return oldestSource;
            }
        }
        
        Debug.LogWarning("[AudioManager] AudioSource 池已满，无法分配新的音频源");
        return null;
    }
    
    // 将 AudioSource 返回到池中
    private void ReturnAudioSourceToPool(AudioSource source, bool immediate = false)
    {
        if (source == null) return;
        
        if (immediate)
        {
            ReturnAudioSourceImmediate(source);
        }
        else
        {
            StartCoroutine(ReturnAudioSourceWhenFinished(source));
        }
    }
    
    // 立即返回 AudioSource 到池中
    private void ReturnAudioSourceImmediate(AudioSource source)
    {
        if (source == null) return;
        
        source.Stop();
        ClearAudioSource(source);
        source.enabled = false;
        
        if (activeAudioSources.Contains(source))
        {
            activeAudioSources.Remove(source);
        }
        
        // 如果池未满，返回池中
        if (audioSourcePool.Count < maxPoolSize)
        {
            audioSourcePool.Enqueue(source);
        }
        else
        {
            // 池已满，销毁多余的 AudioSource
            Destroy(source);
        }
    }
    
    // 清理 AudioSource 的设置
    private void ClearAudioSource(AudioSource source)
    {
        source.clip = null;
        source.outputAudioMixerGroup = null;
        source.volume = 1f;
        source.pitch = 1f;
        source.loop = false;
        source.time = 0f;
    }
    
    // 等待音频播放完毕后返回池中
    private IEnumerator ReturnAudioSourceWhenFinished(AudioSource source)
    {
        if (source == null || source.loop) yield break;
        
        float clipLength = source.clip != null ? source.clip.length : 0f;
        
        if (clipLength > 0)
        {
            // 等待音频播放完毕（考虑音调影响）
            float playTime = clipLength / Mathf.Abs(source.pitch);
            yield return new WaitForSeconds(playTime);
            
            // 检查音频是否仍在播放
            if (source != null && source.isPlaying)
            {
                // 如果仍在播放，继续等待
                while (source != null && source.isPlaying)
                {
                    yield return null;
                }
            }
        }
        
        // 等待一帧确保音频确实播放完毕
        yield return null;
        
        // 返回池中
        if (source != null)
        {
            ReturnAudioSourceImmediate(source);
        }
    }
    
    // 从Resources加载所有AudioType SO
    private void LoadAudioTypesFromResources()
    {
        audioTypes.Clear();
        
        AudioType[] loadedTypes = Resources.LoadAll<AudioType>(audioResourcesPath);
        
        if (loadedTypes.Length == 0)
        {
            Debug.LogWarning($"[AudioManager] 在 Resources/{audioResourcesPath} 里没有找到任何 AudioType ScriptableObject！");
            return;
        }
        
        audioTypes.AddRange(loadedTypes);
        Debug.Log($"[AudioManager] 已从 Resources/{audioResourcesPath} 加载 {audioTypes.Count} 个 AudioType。");
    }
    
    // 初始化音频字典
    private void InitializeAudioDictionary()
    {
        audioDictionary.Clear();
        
        foreach (AudioType audioType in audioTypes)
        {
            if (audioType != null && audioType.clip != null)
            {
                if (!audioDictionary.ContainsKey(audioType.name))
                {
                    audioDictionary.Add(audioType.name, audioType);
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] 存在重名的 AudioType: {audioType.name}");
                }
            }
        }
    }
    
    private void Start()
    {
        // 为每个音频创建 AudioSource（针对非叠加播放的音频）
        foreach (AudioType audioType in audioTypes)
        {
            if (audioType == null || audioType.clip == null) continue;
            
            // 对于允许叠加播放的音频，我们将在播放时动态分配AudioSource
            if (audioType.allowMultipleInstances) continue;
            
            // 为不允许叠加的音频分配固定的AudioSource
            AudioSource source = GetAudioSourceFromPool();
            if (source != null)
            {
                SetupAudioSource(source, audioType);
                audioType.source = source;
                
                // 如果需要，播放音频
                if (audioType.playOnAwake)
                {
                    source.Play();
                }
            }
        }
    }
    
    // 设置 AudioSource 参数
    private void SetupAudioSource(AudioSource source, AudioType audioType, bool applyRandomPitch = true)
    {
        source.clip = audioType.clip;
        source.volume = audioType.volume;
        
        // 设置音调
        if (audioType.useRandomPitch && applyRandomPitch)
        {
            source.pitch = audioType.GetEffectivePitch();
        }
        else
        {
            source.pitch = audioType.pitch;
        }
        
        source.loop = audioType.loop;
        
        if (audioType.mixerGroup != null)
        {
            source.outputAudioMixerGroup = audioType.mixerGroup;
        }
    }
    
    // 播放音频（基础方法）
    public void Play(string audioTypeName)
    {
        if (!audioDictionary.ContainsKey(audioTypeName))
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
            return;
        }
        
        AudioType audioType = audioDictionary[audioTypeName];
        
        // 检查是否允许叠加播放
        if (audioType.allowMultipleInstances)
        {
            PlayMultiple(audioType);
        }
        else
        {
            PlaySingle(audioType);
        }
    }
    
    // 播放单个实例（不允许叠加）
    private void PlaySingle(AudioType audioType, bool applyRandomPitch = true)
    {
        if (audioType.source == null)
        {
            AudioSource source = GetAudioSourceFromPool();
            if (source == null) return;
            
            SetupAudioSource(source, audioType, applyRandomPitch);
            audioType.source = source;
        }
        else
        {
            // 如果已经有AudioSource，更新音调
            if (audioType.useRandomPitch && applyRandomPitch)
            {
                audioType.source.pitch = audioType.GetEffectivePitch();
            }
            else if (!applyRandomPitch)
            {
                audioType.source.pitch = audioType.pitch;
            }
            
            // 如果音频已在播放中，重新开始播放
            if (audioType.source.isPlaying)
            {
                audioType.source.time = 0f;
            }
        }
        
        if (audioType.source != null)
        {
            audioType.source.Play();
        }
    }
    
    // 播放多个实例（允许叠加）
    private void PlayMultiple(AudioType audioType, bool applyRandomPitch = true)
    {
        // 从池中获取 AudioSource
        AudioSource newSource = GetAudioSourceFromPool();
        
        if (newSource != null)
        {
            // 设置音频参数
            SetupAudioSource(newSource, audioType, applyRandomPitch);
            
            // 初始化实例列表（如果需要）
            if (!audioInstances.ContainsKey(audioType.name))
            {
                audioInstances[audioType.name] = new List<AudioSource>();
            }
            
            // 添加到实例列表
            audioInstances[audioType.name].Add(newSource);
            
            // 播放音频
            newSource.Play();
            
            // 对于非循环音频，播放完毕后自动回收
            if (!audioType.loop)
            {
                StartCoroutine(CleanupAudioSourceWhenFinished(newSource, audioType.name));
            }
        }
    }
    
    // 清理已播放完毕的音频实例
    private IEnumerator CleanupAudioSourceWhenFinished(AudioSource source, string audioName)
    {
        if (source == null) yield break;
        
        // 等待音频播放完毕
        yield return StartCoroutine(WaitForAudioFinish(source));
        
        // 从实例列表中移除
        if (audioInstances.ContainsKey(audioName) && source != null)
        {
            audioInstances[audioName].Remove(source);
            
            // 如果列表为空，移除键
            if (audioInstances[audioName].Count == 0)
            {
                audioInstances.Remove(audioName);
            }
        }
        
        // 返回 AudioSource 到池中
        ReturnAudioSourceToPool(source, true);
    }
    
    // 等待音频播放完毕
    private IEnumerator WaitForAudioFinish(AudioSource source)
    {
        if (source == null || source.loop) yield break;
        
        if (source.clip != null)
        {
            // 计算播放时间（考虑音调）
            float playTime = source.clip.length / Mathf.Abs(source.pitch);
            yield return new WaitForSeconds(playTime);
        }
        
        // 额外等待一帧确保音频结束
        yield return null;
    }
    
    // 播放音频（带自定义音调）
    public void PlayWithCustomPitch(string audioTypeName, float customPitch)
    {
        if (!audioDictionary.ContainsKey(audioTypeName))
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
            return;
        }
        
        AudioType audioType = audioDictionary[audioTypeName];
        
        // 临时存储原始音调设置
        bool originalUseRandomPitch = audioType.useRandomPitch;
        float originalPitch = audioType.pitch;
        
        // 使用自定义音调
        audioType.useRandomPitch = false;
        audioType.pitch = Mathf.Clamp(customPitch, 0.1f, 3f);
        
        // 播放音频
        if (audioType.allowMultipleInstances)
        {
            PlayMultiple(audioType, false);
        }
        else
        {
            PlaySingle(audioType, false);
        }
        
        // 恢复原始设置（如果音频是单实例的）
        if (!audioType.allowMultipleInstances)
        {
            audioType.useRandomPitch = originalUseRandomPitch;
            audioType.pitch = originalPitch;
        }
    }
    
    // 暂停音频
    public void Pause(string audioTypeName)
    {
        if (audioDictionary.ContainsKey(audioTypeName))
        {
            AudioType audioType = audioDictionary[audioTypeName];
            
            if (!audioType.allowMultipleInstances)
            {
                if (audioType.source != null)
                {
                    audioType.source.Pause();
                }
            }
            else
            {
                // 暂停所有实例
                if (audioInstances.ContainsKey(audioTypeName))
                {
                    foreach (AudioSource source in audioInstances[audioTypeName])
                    {
                        if (source != null) source.Pause();
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
        }
    }
    
    // 停止音频
    public void Stop(string audioTypeName)
    {
        if (audioDictionary.ContainsKey(audioTypeName))
        {
            AudioType audioType = audioDictionary[audioTypeName];
            
            if (!audioType.allowMultipleInstances)
            {
                if (audioType.source != null)
                {
                    audioType.source.Stop();
                    // 对于单实例音频，不清除 AudioSource，保留配置
                }
            }
            else
            {
                // 停止所有实例并回收 AudioSource
                if (audioInstances.ContainsKey(audioTypeName))
                {
                    List<AudioSource> sourcesToRemove = new List<AudioSource>(audioInstances[audioTypeName]);
                    
                    foreach (AudioSource source in sourcesToRemove)
                    {
                        if (source != null)
                        {
                            source.Stop();
                            ReturnAudioSourceToPool(source, true);
                        }
                    }
                    audioInstances.Remove(audioTypeName);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
        }
    }
    
    // 设置音频音量
    public void SetVolume(string audioTypeName, float volume)
    {
        if (audioDictionary.ContainsKey(audioTypeName))
        {
            AudioType audioType = audioDictionary[audioTypeName];
            audioType.volume = Mathf.Clamp01(volume);
            
            if (!audioType.allowMultipleInstances)
            {
                if (audioType.source != null)
                {
                    audioType.source.volume = audioType.volume;
                }
            }
            else
            {
                // 设置所有实例的音量
                if (audioInstances.ContainsKey(audioTypeName))
                {
                    foreach (AudioSource source in audioInstances[audioTypeName])
                    {
                        if (source != null) source.volume = audioType.volume;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
        }
    }
    
    // 设置音频音调（非随机）
    public void SetPitch(string audioTypeName, float pitch)
    {
        if (audioDictionary.ContainsKey(audioTypeName))
        {
            AudioType audioType = audioDictionary[audioTypeName];
            audioType.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            
            if (!audioType.allowMultipleInstances)
            {
                if (audioType.source != null)
                {
                    audioType.source.pitch = audioType.pitch;
                }
            }
            else
            {
                // 设置所有实例的音调
                if (audioInstances.ContainsKey(audioTypeName))
                {
                    foreach (AudioSource source in audioInstances[audioTypeName])
                    {
                        if (source != null) source.pitch = audioType.pitch;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
        }
    }
    
    // 为音频生成随机音调（仅对启用随机音调的音频有效）
    public void ApplyRandomPitch(string audioTypeName)
    {
        if (audioDictionary.ContainsKey(audioTypeName))
        {
            AudioType audioType = audioDictionary[audioTypeName];
            
            if (audioType.useRandomPitch)
            {
                float randomPitch = audioType.GetEffectivePitch();
                
                if (!audioType.allowMultipleInstances)
                {
                    if (audioType.source != null)
                    {
                        audioType.source.pitch = randomPitch;
                    }
                }
                else
                {
                    // 为所有实例应用随机音调
                    if (audioInstances.ContainsKey(audioTypeName))
                    {
                        foreach (AudioSource source in audioInstances[audioTypeName])
                        {
                            if (source != null) source.pitch = randomPitch;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到名为 '{audioTypeName}' 的音频类型！");
        }
    }
    
    // 检查音频是否正在播放
    public bool IsPlaying(string audioTypeName)
    {
        if (!audioDictionary.ContainsKey(audioTypeName)) return false;
        
        AudioType audioType = audioDictionary[audioTypeName];
        
        if (!audioType.allowMultipleInstances)
        {
            return audioType.source != null && audioType.source.isPlaying;
        }
        else
        {
            // 检查是否有任何实例正在播放
            if (audioInstances.ContainsKey(audioTypeName))
            {
                foreach (AudioSource source in audioInstances[audioTypeName])
                {
                    if (source != null && source.isPlaying) return true;
                }
            }
            return false;
        }
    }
    
    // 获取所有已加载的音频名称
    public List<string> GetAllAudioNames()
    {
        return new List<string>(audioDictionary.Keys);
    }
    
    // 获取音频的当前音调
    public float GetCurrentPitch(string audioTypeName)
    {
        if (!audioDictionary.ContainsKey(audioTypeName)) return 1f;
        
        AudioType audioType = audioDictionary[audioTypeName];
        
        if (!audioType.allowMultipleInstances)
        {
            return audioType.source != null ? audioType.source.pitch : audioType.pitch;
        }
        else
        {
            // 返回第一个实例的音调，如果没有实例则返回配置的音调
            if (audioInstances.ContainsKey(audioTypeName) && audioInstances[audioTypeName].Count > 0)
            {
                return audioInstances[audioTypeName][0].pitch;
            }
            return audioType.pitch;
        }
    }
    
    // 获取池统计信息（用于调试）
    public void GetPoolStats(out int poolCount, out int activeCount, out int totalCount)
    {
        poolCount = audioSourcePool.Count;
        activeCount = activeAudioSources.Count;
        totalCount = poolCount + activeCount;
    }
    
    // 清理所有音频实例
    public void CleanupAllAudioInstances()
    {
        // 清理所有多实例音频
        foreach (var kvp in audioInstances)
        {
            foreach (AudioSource source in kvp.Value)
            {
                if (source != null)
                {
                    source.Stop();
                    ReturnAudioSourceToPool(source, true);
                }
            }
        }
        audioInstances.Clear();
        
        // 清理所有单实例音频（保留 AudioSource 但不播放）
        foreach (AudioType audioType in audioTypes)
        {
            if (audioType != null && audioType.source != null && !audioType.allowMultipleInstances)
            {
                audioType.source.Stop();
            }
        }
    }
    
    private void OnDestroy()
    {
        CleanupAllAudioInstances();
    }
    
    // 编辑器工具：重新加载音频
    [ContextMenu("重新加载音频")]
    public void ReloadAudioTypes()
    {
        CleanupAllAudioInstances();
        LoadAudioTypesFromResources();
        InitializeAudioDictionary();
        
        // 重新初始化单实例音频
        foreach (AudioType audioType in audioTypes)
        {
            if (audioType == null || audioType.clip == null) continue;
            if (audioType.allowMultipleInstances) continue;
            
            AudioSource source = GetAudioSourceFromPool();
            if (source != null)
            {
                SetupAudioSource(source, audioType);
                audioType.source = source;
            }
        }
        
        Debug.Log("[AudioManager] 音频已重新加载。");
    }
}