using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamesMing
{
    /*
    第一版
    完成身体移动
    身体移动时，向目标方向倾斜(通过delmove计算倾斜)
     */
    public class SpiderIkController_V_1 : MonoBehaviour
    {
        [Range(0f, 1f)] public float neckExtension = 1;
        [Range(01, 1f)] public float crouchFactor = 0;// 蹲伏因子
        public float headRotation;
        public bool ragdoll;
        public float crouchSpanFactor;// 蹲下跨度因子
        public float constraintRadiusMin;// 约束半径最小值
        public float constraintRadiusMax;// 约束半径最大值
        public float constraintRadiusMaxSpeed;// 约束半径最大速度
        public float liftHeight;// 抬起高度
        public float legSpeedFactor;// 腿速度因子
        public float legMinSpeed;// 腿部最小速度
        public float movementResponseFrequency = 2;// 运动响应频率
        public float movementResponseDamping = 1f;// 运动响应阻尼
        public float movementResponseUndershoot = 0f;// 运动响应下冲
        private float f, z, r;// 运动响应参数变化检测
        public float speedSmoothingTime = 0.3f;// 运动倾斜速度 平滑时间
        public float movementTiltFactor = 100;// 运动倾斜因子
        public float movementTiltMax = 10;//运动最大倾斜
        public LayerMask CollsionLayer;
        public Transform frontLeftLeg, frontRightLeg, backLeftLeg, backRightLeg, body;
        /// <summary> 目标位置/标记位置 </summary>
        public Transform mark;

        private SecondOrderDynamics_V_5 body_SOD_m;

        private void Awake()
        {
            body_SOD_m = new SecondOrderDynamics_V_5();
            body_SOD_m.SetInitState(body.position);
            CheckMovementResponseFactorChange();
        }

        public void Update()
        {
            CheckMovementResponseFactorChange();
            body.position = body_SOD_m.Update(Time.deltaTime, mark.position, out var delmove);
            CaculateMovementTilt_delpos(delmove);
        }

        private void CheckMovementResponseFactorChange()
        {
            if (f != movementResponseFrequency || z != movementResponseDamping || r != movementResponseUndershoot) {
                f = movementResponseFrequency;
                z = movementResponseDamping;
                r = movementResponseUndershoot;
                body_SOD_m.SetConstants(movementResponseFrequency, movementResponseDamping, movementResponseUndershoot);
            }
        }

        private void CaculateMovementTilt_delpos(Vector3 delMove)
        {
            Vector3 tarEul = new Vector3(0, body.localEulerAngles.y, 0);// 初始值为xz轴无旋转
            // 不知道怎么算 ，最后取巧整了个这个
            // 蜘蛛身体正上方  到   移动方向的旋转 1度 的单位向量
            //Vector3 rotateDir = Quaternion.FromToRotation(body.up, delMove.normalized).normalized.eulerAngles;
            // 单位倾斜矢量，通过单位位移方向，对应旋转轴
            Vector3 localVer = body.InverseTransformVector(delMove).normalized;
            Vector3 rotateDir = new Vector3(localVer.z, 0, -localVer.x).normalized;
            // 旋转长 最大度数限制
            float length = Mathf.Min(delMove.magnitude * movementTiltFactor, movementTiltMax);
            // 目标位置
            tarEul += rotateDir * length;
            Debug.DrawLine(body.position, body.position + delMove.normalized * 5, Color.green, 0.05f);
            // 计算 应用， 使用四元数，用欧拉角会抖动
            body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(tarEul), speedSmoothingTime);
        }
    }
}