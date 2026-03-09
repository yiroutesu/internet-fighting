using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewAudioType", menuName = "Audio/Audio Type")]
[System.Serializable]
public class AudioType : ScriptableObject
{
    [Header("音频资源")]
    public AudioClip clip;
    
    [Header("音频设置")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("音调设置")]
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    
    [Tooltip("是否启用随机音调")]
    public bool useRandomPitch = false;
    
    [Tooltip("随机音调最小值 (当 useRandomPitch 为 true 时有效)")]
    [Range(0.1f, 3f)]
    public float randomPitchMin = 0.9f;
    
    [Tooltip("随机音调最大值 (当 useRandomPitch 为 true 时有效)")]
    [Range(0.1f, 3f)]
    public float randomPitchMax = 1.1f;
    
    [Header("其他设置")]
    public bool loop = false;
    public AudioMixerGroup mixerGroup;
    public bool playOnAwake = false;
    
    [Tooltip("是否允许音频叠加播放（允许多个相同音频同时播放）")]
    public bool allowMultipleInstances = false;
    
    [HideInInspector]
    public AudioSource source;
    
    // 获取有效的音调值（如果启用随机，则返回随机值）
    public float GetEffectivePitch()
    {
        if (useRandomPitch)
        {
            return Random.Range(randomPitchMin, randomPitchMax);
        }
        return pitch;
    }
}