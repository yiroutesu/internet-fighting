using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitPlayerMovement : MonoBehaviour
{
    public Transform activityArea;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 safePosition = activityArea.GetComponent<Collider>().bounds.ClosestPoint(this.transform.position);
            transform.position = safePosition;
        }
    }
}
