using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamesMing;


/// <summary>
/// 使用SecondOrderDynamics_V_2处理脚步ik
/// </summary>
public class SpiderFeet : MonoBehaviour
{
    public Transform body;                  //身体
    public LayerMask terrainLayer;          //检测图层
    Vector3 newposition, oldposition, currentposition; //位置

    public float footSpacing1, footSpacing2; //落点偏移
    public float stepstance;                //步长
    public float high = 0.1f;               //高度
    public float speed = 2;                 //速度
    float lerp = 1;

    public SpiderFeet leg1, leg2;                //约束

    public float f, z, r;
    private float lf, lz, lr;

    private SecondOrderDynamics_V_2 alllll;

    private void Start()
    {
        alllll = new SecondOrderDynamics_V_2();
        alllll.SetInitPos(transform.position);
        lf = f;
        lz = z;
        lr = r;
        alllll.SetConstants(f, z, r);
        newposition = transform.position;
        currentposition = transform.position;
    }

    void Update()
    {
        if (f != lf || z != lz || r != lr) 
        {
            lf = f;
            lz = z;
            lr = r;
            alllll.SetConstants(f, z, r);
        }
        transform.position = alllll.Update(Time.deltaTime, currentposition);
        Ray ray = new Ray(body.position + (body.up * footSpacing1) + (body.right * footSpacing2), -body.forward);
        if (Physics.Raycast(ray, out RaycastHit info, 10, terrainLayer.value))
        {
            if (Vector3.Distance(newposition, info.point) > stepstance && leg1.lerp >= 1 && leg2.lerp >= 1)
            {
                lerp = 0;
                newposition = info.point;
            }
        }

        if (lerp < 1)
        {
            Vector3 footposition = Vector3.Lerp(oldposition, newposition, lerp);
            footposition.y += Mathf.Sin(lerp * Mathf.PI) * high;
            currentposition = footposition;
            lerp += Time.deltaTime * speed;
        }
        else
        {
             oldposition = newposition;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newposition, 0.2f);
    }
}
