using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerController;

public class PlayerTilt : MonoBehaviour
{
    public PlayerController playerController;
    public float maxTiltAngels=10;//最大倾斜角
    public float tiltSpeed=5;//倾斜速度
    public float resetSpeed=20;
    public bool onlyTiltWhenMoving = true;//是否只在移动时倾斜
    public Vector3 targetRotation;//目标倾斜角

    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No rigidbody found");
        }
        targetRotation = transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDirection = GetMovementDirection();
        bool isMoving = playerController.inputDirection.x != 0 || playerController.inputDirection.z != 0;
        //不移动时重置
        if (onlyTiltWhenMoving && !isMoving)
        {
            ResetTilt();
            return;
        }
        //移动时获取朝向
        if (isMoving)
        {
            float forwardTilt=-moveDirection.z*maxTiltAngels;
            float sideTilt=-moveDirection.x*maxTiltAngels;
            targetRotation=new Vector3(Mathf.Clamp(sideTilt,-maxTiltAngels,maxTiltAngels),
                transform.eulerAngles.y,
                Mathf.Clamp(forwardTilt,-maxTiltAngels,maxTiltAngels));
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(targetRotation),
            tiltSpeed * Time.deltaTime
        );

    }

    //获取移动朝向
    Vector3 GetMovementDirection()
    {
        if (rb!=null)
        {
            return rb.velocity.normalized;
        }
        else
        {
            return Vector3.zero;
        }
    }

    //重置倾斜角度
    private void ResetTilt()
    {
        targetRotation = new Vector3(0, -90, 0);
        
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(targetRotation),
            resetSpeed * Time.deltaTime
        );
    }
}
