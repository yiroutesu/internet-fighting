using Unity.Mathematics;
using UnityEngine;

namespace GamesMing
{
    /// <summary>
    /// 零点匹配法 原版副本
    /// </summary>
    public class SecondOrderDynamics_V_6
    {
        /// <summary> previous input  先前的输入 </summary>
        private Vector3 xp;
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
        private float _w, _z, _d;

        public void SetConstants(float f, float z, float r)
        {
            // compute constants   计算常数
            _w = 2 * Mathf.PI * f;
            _z = z;
            _d = _w * Mathf.Sqrt(Mathf.Abs(z * z - 1));
            k1 = z / (Mathf.PI * f);
            k2 = 1f / (_w * _w);
            k3 = r * z / _w;
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
            if (xd == Vector3.zero) { // estimate velocity  估计速度
                xd = (x - xp) / T;
                xp = x;
            }
            float k1_stable, k2_stable;
            if (_w * T < _z) {// clamp k2 to guarantee stability without gitter  限制 k2 以保证稳定性，无需 gitter
                k1_stable = k1;
                k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);
            }
            else { // use pole matching when the system is very fast  当系统非常快时使用零点匹配
                float t1 = Mathf.Exp(-_z * _w * T);
                float alpha = 2 * t1 * (_z <= 1 ? math.cos(T * _d) : math.cosh(T * _d));
                float beta = t1 * t1;
                float t2 = T / (1 + beta - alpha);
                k1_stable = (1 - beta) * t2;
                k2_stable = T * t2;
            }

            y = y + T * yd;// integrate position by velocity  按速度积分位置
            yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2_stable;// integrate velocity by acceleration 通过加速度积分速度
            return y;
        }
    }
}