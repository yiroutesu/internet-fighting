// 简单的粒子系统配置脚本
using UnityEngine;

public class LaserTrailEffect : MonoBehaviour
{
    public ParticleSystem trailParticles;
    public Color trailColor = new Color(1, 0.3f, 0.1f, 0.5f);
    
    void Start()
    {
        if (trailParticles != null)
        {
            var main = trailParticles.main;
            main.startColor = trailColor;
            main.startSize = 0.1f;
            main.startSpeed = -2f; // 向后发射
        }
    }
}