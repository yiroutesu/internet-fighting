using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector3 = UnityEngine.Vector3;
using SIGame.Enums;

public class PlayerController : MonoBehaviour
{
    public static PlayerInputControls InputControls { get; private set; }
    public Vector3 inputDirection;
    private Rigidbody rb;
    public float speed;
    public float smoothTime = 0.1f;
    private Vector3 currentVelocity = Vector3.zero;
    public Transform playGround;
    private Bounds bounds;
    public StatSystem statSystem;

    // ✅ 已移除：isBeingLaunched, launchVelocity, launchDrag 等

    private void Awake()
    {
        InputControls = new PlayerInputControls();
        rb = GetComponent<Rigidbody>();
        bounds = playGround.GetComponent<Collider>().bounds;
        statSystem = GetComponent<StatSystem>();
        if (statSystem == null)
            Debug.LogError("Player missing StatSystem!", this);

        if (rb != null)
            rb.freezeRotation = true;
    }

    private void OnEnable() => InputControls.Enable();
    private void OnDisable() => InputControls.Disable();

    private void Update()
    {
        inputDirection = InputControls.Gameplay.Move.ReadValue<Vector3>();
    }

    private void FixedUpdate()
    {
        // ✅ 始终正常移动 + 边界限制
        Vector3 clampedPos = bounds.ClosestPoint(transform.position);
        transform.position = clampedPos;
        Move();
    }

    public void Move()
    {
        Vector3 targetDirection = new Vector3(
            inputDirection.x - inputDirection.z,
            0f,
            inputDirection.x + inputDirection.z
        ).normalized;

        // 如果输入为零，方向会是 (0,0,0)，此时不移动
        if (targetDirection == Vector3.zero)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        float currentMoveSpeed = statSystem != null 
            ? statSystem.GetValue(PlayerStatAttr.MoveSpeed) 
            : speed;

        Vector3 targetVelocity = new Vector3(
            targetDirection.x * currentMoveSpeed,
            rb.velocity.y,
            targetDirection.z * currentMoveSpeed
        );

        Vector3 smoothedVelocity = Vector3.SmoothDamp(
            new Vector3(rb.velocity.x, 0, rb.velocity.z),
            new Vector3(targetVelocity.x, 0, targetVelocity.z),
            ref currentVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        rb.velocity = new Vector3(smoothedVelocity.x, rb.velocity.y, smoothedVelocity.z);
    }

    public void Die()
    {
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
    }
    // ✅ 可选：保留空方法以避免其他脚本报错（如 Boss 调用）
    // 如果确定没有地方调用，可完全删除
    public void LaunchAlongDirection(Vector3 direction, float power, float duration = 0.8f)
    {
        // Do nothing — 击飞已被禁用
    }
}