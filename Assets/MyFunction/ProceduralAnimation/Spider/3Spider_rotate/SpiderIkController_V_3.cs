 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamesMing
{
    /*
    第三版

    旋转时落脚点检测（未完成）
   */
    public class SpiderIkController_V_3 : MonoBehaviour
    {
        #region 用于观察内部数值的字段
        public float speedBody;

        #endregion

        //[Range(0f, 1f)] public float neckExtension = 1;
        //[Range(01, 1f)] public float crouchFactor = 0;// 蹲伏因子
        //public float headRotation;
        //public bool ragdoll = false;

        public float lastYRotate;
        public float limite = 3;
        private float rotateSpeed;
        public float RotateLegSpeedFactor = 10;// 旋转腿部速度因子

        #region 腿部
        [Header("腿部参数")]
        public Vector3 legSpacing = new Vector3(1.5f, -0.5f, 1f);// 各脚之间间距
        private Vector3 legspacing;
        public float CrouchSpanFactor = 3;// 蹲下跨度因子
        public float ConstraintRadiusMin = 0.5f;// 约束半径最小值
        public float ConstraintRadiusMax = 1f;// 约束半径最大值
        public float ConstraintRadiusMaxSpeed = 4;// 约束半径最大速度
        public float LiftHeight = 0.25f;// 抬起高度
        public float LegSpeedFactor = 3;// 腿速度因子
        public float LegMinSpeed = 4;// 移动腿部最小速度
        public float LegSpeed = 4;
        #endregion

        #region 身体
        [Header("身体参数")]
        public float MovementResponseFrequency = 2;// 运动响应频率
        public float MovementResponseDamping = 0.5f;// 运动响应阻尼
        public float MovementResponseUndershoot = 1.5f;// 运动响应下冲
        private float f, z, r;// 运动响应参数变化检测
        public float SpeedSmoothingTime = 0.04f;// 运动倾斜速度 平滑时间
        public float MovementTiltFactor = 1;// 运动倾斜因子
        public float MovementTiltMax = 10;//运动最大倾斜
        #endregion

        [Header("绑定参数")]
        public LayerMask CollsionLayer;
        public Transform frontLeftLeg, frontRightLeg, backLeftLeg, backRightLeg, body;
        private SpiderLeg flLeg, frLeg, blLeg, brLeg;
        /// <summary> 目标位置/标记位置 </summary>
        public Transform mark;

        private SecondOrderDynamics_V_5 body_SOD_m;

        private void Awake()
        {
            flLeg = new SpiderLeg(frontLeftLeg, 1, -1);
            frLeg = new SpiderLeg(frontRightLeg, 1, 1);
            blLeg = new SpiderLeg(backLeftLeg, -1, -1);
            brLeg = new SpiderLeg(backRightLeg, -1, 1);
            flLeg.SetConstantLeg(frLeg);
            flLeg.SetConstantLeg(blLeg);
            frLeg.SetConstantLeg(flLeg);
            frLeg.SetConstantLeg(brLeg);
            blLeg.SetConstantLeg(brLeg);
            blLeg.SetConstantLeg(flLeg);
            brLeg.SetConstantLeg(blLeg);
            brLeg.SetConstantLeg(frLeg);
            body_SOD_m = new SecondOrderDynamics_V_5();
            body_SOD_m.SetInitState(body.position);
            CheckMovementResponseFactorChange();
        }

        public void Update()
        {
            CheckMovementResponseFactorChange();
            body.position = body_SOD_m.Update(Time.deltaTime, mark.position, out var delmove);
            CaculateMovementTilt_tarPos(mark.position);

            speedBody = body_SOD_m.YD.magnitude;
            MatchLegSpeedWithSpeed(Time.deltaTime);
            MatchLegSpeedWithYRotate(Time.deltaTime);

            CheckStep(flLeg, delmove);
            CheckStep(frLeg, delmove);
            CheckStep(blLeg, delmove);
            CheckStep(brLeg, delmove);
            flLeg.Update(Time.deltaTime, LiftHeight, LegSpeed);
            frLeg.Update(Time.deltaTime, LiftHeight, LegSpeed);
            blLeg.Update(Time.deltaTime, LiftHeight, LegSpeed);
            brLeg.Update(Time.deltaTime, LiftHeight, LegSpeed);
        }

        /// <summary> 脚部运动检测 </summary>
        private void CheckStep(SpiderLeg leg, Vector3 delmove)
        {
            if (leg.Active)
                return;
            Vector3 center = leg.Center.position;
            float dis = Vector3.Distance(center, leg.Leg.position);
            if (dis > ConstraintRadiusMin)// 最小约束，可以尝试移动
            {
                if (dis > ConstraintRadiusMax)// 最大约束，直接移动
                    goto ATU;
                else if (leg.CanAcive() == false)
                    return;
                ATU:;//  重新计算落脚点, 并开始抬脚
                Vector3 del = delmove.magnitude >= 0.005f ? GetDelMove() : Vector3.zero;
                del += body.up * 1.5f;

                Ray ray = new Ray(center, -body.up);
                //Debug.DrawLine(center + del, center + del - body.up * 3, Color.red, 1f);// 落脚点射线可视化
                if (Physics.Raycast(ray, out RaycastHit info, 4, CollsionLayer))
                {
                    leg.SetTarPosAndActive(info.point);// 检测到落脚点才激活脚部移动，如果没有检测到，则下一帧继续检测
                }

                Vector3 GetDelMove()
                {// 根据身体当前移动方向判断，补正落脚点方向
                    return delmove.normalized * GetCorrection(delmove);
                }
            }
        }

        #region function
        /// <summary> 脚步补正距离（如人向前走，落脚点会在身体中线往前一点距离） </summary>
        private float GetCorrection(Vector3 delmove)
        {
            // 先根据身体的移动速度，判断是否进行落脚点补正，并对最大补正距离限制
            float temp = body_SOD_m.YD.magnitude;
            float length = Mathf.Min(temp > 0.2f ? ConstraintRadiusMin + temp * 0.1f : (rotateSpeed > limite ? ConstraintRadiusMin + rotateSpeed * 0.1f : 0), ConstraintRadiusMax);
            return length;
        }

        private void MatchLegSpeedWithSpeed(float deltime)
        {// 根据身体移动速度，调整脚部速度
            float speedc = body_SOD_m.YD.magnitude - LegSpeed;
            LegSpeed = Mathf.Max(LegSpeed + speedc * deltime * LegSpeedFactor, LegMinSpeed);
        }

        private void MatchLegSpeedWithYRotate(float deltime)
        {
            float rotateDifference = Mathf.Abs(body.localEulerAngles.y - lastYRotate);// 角度变化
            lastYRotate = body.localEulerAngles.y;
            float radius = new Vector2(legSpacing.x, legSpacing.z).magnitude;// 半径
            rotateSpeed = Mathf.PI * radius * rotateDifference / 180f / Time.deltaTime;
            LegSpeed = Mathf.Max(LegSpeed + rotateSpeed * deltime * RotateLegSpeedFactor, LegMinSpeed);
        }

        // 参数变化检测
        private void CheckMovementResponseFactorChange()
        {
            if (f != MovementResponseFrequency || z != MovementResponseDamping || r != MovementResponseUndershoot)
            {
                f = MovementResponseFrequency;
                z = MovementResponseDamping;
                r = MovementResponseUndershoot;
                body_SOD_m.SetConstants(MovementResponseFrequency, MovementResponseDamping, MovementResponseUndershoot);
            }
            if (legspacing != legSpacing)
            {
                legspacing = legSpacing;
                flLeg.SetCenter(body, legSpacing);
                frLeg.SetCenter(body, legSpacing);
                blLeg.SetCenter(body, legSpacing);
                brLeg.SetCenter(body, legSpacing);
            }
        }
        // 运动时身体倾斜
        private void CaculateMovementTilt_tarPos(Vector3 tarPos)
        {
            Vector3 tarEul = new Vector3(0, body.localEulerAngles.y, 0);// 初始值为xz轴无旋转
            // 不知道怎么算 ，最后取巧整了个这个
            // 单位倾斜矢量，通过单位位移方向，对应赋值旋转轴
            Vector3 temVer = tarPos - body.position;
            if (temVer.magnitude > 0.2f)
            {
                Vector3 localMove = body.InverseTransformVector(temVer).normalized;
                Vector3 rotateDir = new Vector3(localMove.z, 0, -localMove.x).normalized;
                // 加上倾斜得到最终目标旋转
                tarEul += rotateDir * MovementTiltMax;
                //Debug.DrawLine(body.position, body.position + temVer.normalized * 5, Color.green, 0.05f);// 移动方向，倾斜方向
            }
            // 计算 应用， 使用四元数，用欧拉角会抖动
            body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(tarEul), SpeedSmoothingTime);
        }
        #endregion
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
           
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                DrawFootRange(body.position + legSpacing.x * body.right + legSpacing.z * body.forward + body.up * legSpacing.y);
                DrawFootRange(body.position + legSpacing.x * body.right - legSpacing.z * body.forward + body.up * legSpacing.y);
                DrawFootRange(body.position - legSpacing.x * body.right + legSpacing.z * body.forward + body.up * legSpacing.y);
                DrawFootRange(body.position - legSpacing.x * body.right - legSpacing.z * body.forward + body.up * legSpacing.y);
                flLeg.OnGizmoDrawPoint();
                frLeg.OnGizmoDrawPoint();
                blLeg.OnGizmoDrawPoint();
                brLeg.OnGizmoDrawPoint();
            }

            void DrawFootRange(Vector3 center)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(center, ConstraintRadiusMin);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(center, ConstraintRadiusMax);
            }
        }
#endif
        #region 将腿部控制类，设为内部类，方便版本区分
        public class SpiderLeg
        {
            public Transform Leg;
            public Transform Center { get; private set; }// 脚底 在body周围的 默认位置
            public bool Active { get; private set; }
            public Vector3 tarPos;// 本次计算的落脚点
            private List<SpiderLeg> list_constantLeg = new List<SpiderLeg>();// 约束腿,约束腿和被约束腿不会同时 active
            private float lerp;
            private Vector3 oldPos;// 上一次计算的落脚点

            private int fb;// 前后  1为前，-1为后
            private int rl;// 右左  1为右，-1为左

            public SpiderLeg(Transform leg, int fb, int rl)
            {
                this.Leg = leg;
                this.fb = fb;
                this.rl = rl;
                tarPos = leg.position;
            }

            /// <summary> 脚，以身体为中心的某个固定位置，为待机position </summary>
            public void SetCenter(Transform body, Vector3 legSpacing)
            {
                if (Center == null)
                {
                    GameObject obj = new GameObject(Leg.name + "_Center");
                    obj.transform.parent = body;
                    Center = obj.transform;
                }
                Center.position = body.position + rl * legSpacing.x * body.right + fb * legSpacing.z * body.forward + body.up * legSpacing.y;
            }

            public void SetConstantLeg(SpiderLeg leg)
            {
                if (list_constantLeg.Contains(leg) == false)
                    list_constantLeg.Add(leg);
            }

            public void Update(float delTime, float high, float speed)
            {
                Vector3 footposition = tarPos;
                if (Active)
                {
                    lerp += delTime * speed;
                    if (lerp >= 1) Active = false;
                    footposition = Vector3.Lerp(oldPos, tarPos, lerp);
                    footposition.y = Mathf.Sin(lerp * Mathf.PI) * high;
                    //Leg.position = footposition;// 放里面，避免每帧设置position
                }
                Leg.position = footposition;// 每帧固定位置
            }
            /// <summary> 被自己约束的脚 能否活动 </summary>
            public bool CanAcive()
            {
                foreach (var item in list_constantLeg)
                {
                    if (item.Active)
                        return false;
                }
                return true;
            }
            /// <summary> 设置落脚点，并开始活动 </summary>
            public void SetTarPosAndActive(Vector3 point)
            {
                tarPos = point;
                Active = true;
                oldPos = Leg.position;
                lerp = 0;
            }

            public void OnGizmoDrawPoint()
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(tarPos, 0.2f);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(oldPos, 0.2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(Center.position, 0.2f);
            }
        }
        #endregion
    }
}