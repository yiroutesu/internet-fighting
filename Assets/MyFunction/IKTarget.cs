using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTarget : MonoBehaviour
{
[Header("每条腿 3 个关节（hip→knee→ankle）")]
    public Transform[] hip;     //长度=6
    public Transform[] knee;    //长度=6
    public Transform[] ankle;   //长度=6

    [Header("IK Target（空物体即可，长度=6）")]
    public Transform[] legTargets; //把 IK 组件的 Target 拖进来

    [Header("步态参数")]
    public float stepLength = 0.25f;   //脚掌向前迈多远
    public float raycastRange = 1.5f;  //射线长度
    public LayerMask groundMask = -1;  //哪些层算地面

    void LateUpdate()
    {
        for (int i = 0; i < 6; i++)
        {
            if (hip[i] == null || knee[i] == null || ankle[i] == null ||
                legTargets[i] == null) continue;

            //1. 脚掌方向：ankle 的本地 z 轴
            Vector3 footForward = ankle[i].forward;

            //2. 膝盖方向：knee - hip 的向量，决定“膝盖朝哪边弯”
            Vector3 kneeDir = (knee[i].position - hip[i].position).normalized;

            //3. 射线起点：脚掌正下方一点
            Vector3 rayStart = ankle[i].position - footForward * 0.05f
                                + Vector3.up * 0.05f; //稍微抬一点防自碰撞

            RaycastHit hit;
            Vector3 targetPos;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastRange, groundMask))
            {
                //4. 落地点再向前迈半步
                targetPos = hit.point + footForward * stepLength;
            }
            else
            {
                //没打到地面就保持当前高度，只向前迈
                targetPos = rayStart + Vector3.down * raycastRange * 0.5f
                            + footForward * stepLength;
            }

            //5. 写回 IK Target
            legTargets[i].position = targetPos;

            //可选：让 IK Target 的旋转始终和脚掌一致
            legTargets[i].rotation = ankle[i].rotation;
        }
    }

    //调试用
    void OnDrawGizmos()
    {
        if (ankle == null || ankle.Length != 6) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 6; i++)
        {
            if (ankle[i] == null) continue;
            Vector3 origin = ankle[i].position - ankle[i].forward * 0.05f + Vector3.up * 0.05f;
            Gizmos.DrawLine(origin, origin + Vector3.down * raycastRange);
        }
    }
}
