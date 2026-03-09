using UnityEngine;

namespace GamesMing
{
    /// <summary>
    /// 通过多次迭代，减小时间间隔T过长的影响
    /// </summary>
    public class SecondOrderDynamics_V_3
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
        private float T_crit;// critical stable time step  临界稳定时间步长

        public void SetConstants(float f, float z, float r)
        {
            // compute constants   计算常数
            k1 = z / (Mathf.PI * f);
            k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
            k3 = r * z / (2 * Mathf.PI * f);
            T_crit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);// multiply by 0.8f to be safe  乘以 0.8f 以确保安全
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
            int iterations = Mathf.CeilToInt(T / T_crit);// tacke extra interations if T > T_crit  如果 T > T_crit 则进行额外的交互
            T = T / iterations;// each iteration now has a smaller time step  现在每次迭代都有一个更小的时间步长
            for (int i = 0; i < iterations; i++)
            {
                y = y + T * yd;// integrate position by velocity  按速度积分位置
                yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2;// integrate velocity by acceleration 通过加速度积分速度
            }
            return y;
        }
    }

}
