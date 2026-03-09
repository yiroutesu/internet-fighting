using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(TwoBoneIKConstraint))]
public class LegStepper : MonoBehaviour
{
    [Header("Home")]
    public Transform home;

    [Header("Step")]
    public float strideLength = 0.5f;

    [Header("Ground")]
    public LayerMask groundMask = ~0;
    public float rayRange = 10f;

    private Vector3 _wantedPos;
    private Transform _ikTarget;

    void Awake()
    {
        _ikTarget = GetComponent<TwoBoneIKConstraint>().data.target;
        _wantedPos = RayGround(home.position);
        _ikTarget.position = _wantedPos;
        SyncHome();
    }

    void Update()
    {
        // 计算期望落点
        Vector3 fwd = transform.parent.forward * strideLength;
        _wantedPos = RayGround(home.position + fwd);

        // 超出步幅瞬移并更新 home
        if (Vector3.Distance(_ikTarget.position, _wantedPos) > strideLength)
        {
            _ikTarget.position = _wantedPos;
            SyncHome();
        }
    }

    void SyncHome()
    {
        home.position = _ikTarget.position + Vector3.up * 0.2f;
    }

    Vector3 RayGround(Vector3 from)
    {
        return Physics.Raycast(from + Vector3.up * 0.1f, Vector3.down, out RaycastHit h, rayRange, groundMask)
            ? h.point
            : from;
    }
}