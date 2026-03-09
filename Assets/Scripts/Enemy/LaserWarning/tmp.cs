using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tmp : MonoBehaviour
{
    public LaserWarningController a;
    public LaserWarning b;
    private void OnEnable()
    {
        // a.SetWarning(0,Vector3.forward,10f);
        // a.SetWarning(1,Vector3.forward,10f);
        // a.SetWarning(2,Vector3.forward,10f);
        // a.SetWarning(3,Vector3.forward,10f);
        //a.SetWarningWidth(0,0.5f);
        b.gameObject.SetActive(true);
        b.setWarning(Vector3.zero,transform.position);
    }
}
