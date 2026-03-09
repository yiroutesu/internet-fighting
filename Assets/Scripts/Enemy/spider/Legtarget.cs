using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Legtarget : MonoBehaviour
{
    [Header("脚部 IK 目标")]
    public Transform body;                  // 身体
    public LayerMask terrainLayer;          // 检测图层

    [Header("迈步参数")]
    public float stepstance = 3f;         // 步长
    public float high       = 0.1f;         // 抬脚高度
    public float speed      = 2f;           // 速度

    [Header("初始偏移（世界坐标）")]
    public Vector3 footOffset;              // ← 新增：初始偏移量

    // 内部变量
    Vector3 newPos, oldPos, curPos;
    public float   lerp = 1;

    /* -------------------------------------------------- */

    private void Start()
    {
        curPos = transform.position;
        newPos = curPos;
        footOffset = transform.localPosition;
        Debug.Log(footOffset);
    }

    void Update()
    {
        transform.position = curPos;

        Ray ray = new Ray(body.transform.position + footOffset+new Vector3(0,1,0), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10, terrainLayer))
        {
            Debug.Log(Vector3.Distance(newPos, hit.point));
            if (Vector3.Distance(newPos, hit.point) > stepstance)
            {
                lerp = 0;
                oldPos = newPos;
                newPos = hit.point;
            }
        }

        if (lerp < 1f)
        {
            Vector3 p = Vector3.Lerp(oldPos, newPos, lerp);
            p.y += Mathf.Sin(lerp * Mathf.PI) * high;
            curPos = p;
            lerp += Time.deltaTime * speed;
        }
    }

    /* 可视化 */
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(body.transform.position + footOffset+new Vector3(0,-1,0), 0.2f);
    }
}