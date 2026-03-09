using UnityEngine;

namespace GamesMing
{
    /// <summary>
    /// 通过限制k2,减小时间间隔T过长的影响
    /// </summary>
    public class SecondOrderDynamics_V_4
    {
        private Vector3 xp;// previous input  先前的输入
                           //state variables  状态变量
        /// <summary> 输出 </summary>
        private Vector3 y;
        /// <summary> 输出关于时间的一阶导（斜率/速度） </summary>
        private Vector3 yd;
        //dynamics constants   动力学常数
        /// <summary> 响应速度 </summary>
        private float k1;
        /// <summary> 阻尼 </summary>
        private float k2;
        /// <summary> 初始响应 </summary>
        private float k3;

        public void SetConstants(float f, float z, float r)
        {
            // compute constants   计算常数
            k1 = z / (Mathf.PI * f);
            k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
            k3 = r * z / (2 * Mathf.PI * f);
        }
        public void SetInitPos(Vector3 x0)
        {
            // initialize variables  初始化变量
            xp = x0;// 初始位置x0
            y = x0;// y初始值为初始x0
            yd = Vector3.zero;// yd初始为0
        }

        /// <summary>
        /// 计算当前y
        /// </summary>
        /// <param name="T">deltaTime</param>
        /// <param name="x">当前x</param>
        /// <param name="xd">当前x速度</param>
        /// <returns></returns>
        public Vector3 Update(float T, Vector3 x, Vector3 xd = new Vector3())
        {
            if (xd == Vector3.zero)
            { // estimate velocity  估计速度
                xd = (x - xp) / T;
                xp = x;
            }
            float k2_stable = Mathf.Max(k2, 1.1f * (T * T / 4 + T * k1 / 2));// clamp k2 to guarantee stability  限制 k2 以保证稳定性
                                                                             //float k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);// clamp k2 to guarantee stability without gitter  限制 k2 以保证稳定性，无需 gitter
            y = y + T * yd;// integrate position by velocity  按速度积分位置
            yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2_stable;// integrate velocity by acceleration 通过加速度积分速度
            return y;
        }
    }

}
