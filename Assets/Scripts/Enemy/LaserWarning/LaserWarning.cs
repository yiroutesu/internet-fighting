using UnityEngine;

public class LaserWarning : MonoBehaviour
{

    public void setWarning(Vector3 start,Vector3 end)           
    {
        // 1. 把自己放到中点
        transform.position = (start + end) * 0.5f;

        // 2. 让自身 forward 指向终点
        transform.LookAt(end);

        // 3. 把 localScale.z 设为距离，就能“拉伸”到刚好首尾
        float length = Vector3.Distance(start, end);
        Vector3 scale = transform.localScale;
        scale.z = length;               // 只改 Z
        transform.localScale = scale;
    }
}