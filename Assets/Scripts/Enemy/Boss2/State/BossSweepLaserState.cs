using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BossSweepLaserState : BossStateBase
{
    private static float eachLasertime = 0.5f;
    private int Lasers = 4;

    public override void OnEnter(BossContext ctx)
    {
        ctx.laserWarningController.transform.SetParent(null);
        ctx.laserWarningController.transform.localScale=new Vector3(1,1,1);
        StartCoroutine(SweepLaser(ctx));
    }


    public override void OnUpdate(BossContext ctx)
    {
        
    }
    private IEnumerator SweepLaser(BossContext ctx)
    {
        for (int i = 0; i < Lasers; i++)
        {
            int index = i;
            ctx.StartCoroutine(SweepsoloLaser(ctx, index));
            yield return new WaitForSeconds(eachLasertime);
        }
        yield return new WaitForSeconds(1f);
        if (!ctx.IsDead)
            ctx.GetComponent<BossFSM>().ChangeState<BossIdleState>();
    }

    private IEnumerator SweepsoloLaser(BossContext ctx, int index)
    {
        Vector3 Dir = ctx.PlayerDir;
        ctx.laserWarningController.SetWarning(index, Dir,ctx.transform.position, ctx.laserLength);
        ctx.laserWarningController.SetWarningTick(index, Dir,ctx.transform.position, ctx.laserLength);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / ctx.sweepWarningTime;
            ctx.laserWarningController.SetWarningWidth(index, math.lerp(0, 1, t));
            yield return null;
        }
        //转身
        float duration = 0.05f; 
        Quaternion start = ctx.transform.rotation;
        Quaternion end = Quaternion.LookRotation(new Vector3(Dir.x,0f,Dir.z));
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, elapsed / duration);
            ctx.transform.rotation = Quaternion.Slerp(start, end, p);
            yield return null;
        }
        ctx.transform.rotation = end; // 强制对齐
        GameObject laser=UnityEngine.Object.Instantiate(ctx.Laser);
        laser.transform.position=ctx.transform.position;
        laser.transform.rotation=ctx.transform.rotation;
        laser.SetActive(true);
        
        ctx.anim.MoveFaceAlongNormal(0.15f,CubeController.Face.Back,new Vector3(0.5f,0.3f,1f),0f);
        ctx.anim.StartCombinedAnimation(CubeController.Face.Front, 90f, 300f, 0.15f, new Vector3(0.5f,0.5f,-0.5f),null, 0f);
        // 3. 平滑后撤 0.3 m
        duration = 0.15f;            // 0.15 s 完成位移，可自行调
        Vector3 startvec3 = ctx.transform.position;
        Vector3 endvec3 = startvec3 - Dir * 0.3f;
        float elapse = 0;
        endvec3.y=startvec3.y;
        while (elapse < duration)
        {
            elapse += Time.deltaTime;
            float p = elapse / duration;   // 0~1
            ctx.transform.position = Vector3.Lerp(startvec3, endvec3, p);
            yield return null;
        }
        ctx.transform.position = endvec3;       // 保险：强制到终点

        //yield return new WaitForSeconds(0.15f);
        ctx.laserWarningController.SetFalse(index);
        UnityEngine.Object.Destroy(laser);

    }

}