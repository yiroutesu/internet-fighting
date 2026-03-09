using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserWarningController : MonoBehaviour
{
    private LaserWarning[] laserWarnings;
    private LaserWarningTick[] laserWarningTicks;
    private static float laserWarningHieth=0.1f;

    void Start()
    {
        laserWarnings = GetComponentsInChildren<LaserWarning>();
        laserWarningTicks = GetComponentsInChildren<LaserWarningTick>();
        foreach (var a in laserWarnings)
        {
            a.transform.gameObject.SetActive(false);
        }
        foreach (var a in laserWarningTicks)
        {
            a.transform.gameObject.SetActive(false);
        }
    }
    void Update()
    {
    }
    public void SetWarning(int index, Vector3 dir, Vector3 start, float length)
    {
        Vector3 end = start + dir * length;
        end.y=laserWarningHieth;
        start.y=laserWarningHieth;
        laserWarnings[index].setWarning(start, end);
        laserWarnings[index].gameObject.SetActive(true);
    }
    public void SetWarningTick(int index, Vector3 dir,Vector3 start, float length)
    {
        Vector3 end = start + dir * length;
        end.y=laserWarningHieth;
        start.y=laserWarningHieth;
        laserWarningTicks[index].SetWarning(start, end);
        laserWarningTicks[index].gameObject.SetActive(true);
    }
    public void SetWarningWidth(int index, float width)
    {
        laserWarningTicks[index].SetWidth(width);
    }
    public void SetFalse(int index)
    {
        laserWarnings[index].gameObject.SetActive(false);
        laserWarningTicks[index].gameObject.SetActive(false);
    }
}
