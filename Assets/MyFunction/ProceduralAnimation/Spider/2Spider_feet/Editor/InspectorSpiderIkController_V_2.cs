using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GamesMing
{

    [CustomEditor(typeof(SpiderIkController_V_2))]
    public class InspectorSpiderIkController_V_2 : Editor
    {
        private SpiderIkController_V_2 tar;

        // 修改参数后，重新选中挂载脚本的物体 用来刷新
        private float delTime = 0.016f;// 更新间隔
        private float maxTime = 3f;// 最大时长
        private float startx = 15;// x坐标起始量, 手动设置
        private float starty = 600;// y坐标起始量, 手动设置
        private float d_wid;// 曲线图宽, 代码动态调整大小
        private float d_high = 220f;// 曲线图高, 手动设置
        private float high;
        private float max;// 数据中最大y, 默认为1，如果数据中有比1大的，则用它替换1
        private float min;// 数据中最小y

        private void OnEnable()
        {
            tar = target as SpiderIkController_V_2;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(300);// 腾出空白空间用来画曲线
                                       //return;
            d_wid = EditorGUIUtility.currentViewWidth - startx * 2;// startx * 2表示，左右间隔
                                                                   // 获取数据
            List<float> listY = new List<float>() { 0 };// 默认第一位为 0
            SecondOrderDynamics_V_int SOD = new SecondOrderDynamics_V_int();
            SOD.SetInitState(0);
            SOD.SetConstants(tar.MovementResponseFrequency, tar.MovementResponseDamping, tar.MovementResponseUndershoot);
            float time = 0;
            while (time <= maxTime)
            {
                time += delTime;
                listY.Add(SOD.Update(delTime, 1));
            }
            // 限制曲线图最大范围 系数
            max = 1f;// max 默认值为1f
            min = listY[0];
            foreach (var item in listY)
            {
                if (item > max)
                    max = item;
                if (item < min)
                    min = item;
            }
            high = d_high;// 默认赋值high
            if (max > 0 && min < 0) high = d_high * (max / (max + Mathf.Abs(min)));// 调整 high
            if (max < 0 && min < 0) max = min;// 如果都小于0，用min做系数

            Handles.BeginGUI();

            // 起点vector3.zore 是脚本处在inspector的窗口的左上角, 左->右 => x ->正无穷，上->下 => y -> 正无穷
            // 绘制坐标系
            Handles.color = Color.white;
            Handles.DrawLine(new Vector3(GetPosX(0), GetPosY(0)), new Vector3(GetPosX(maxTime), GetPosY(0)));// 横线
            Handles.DrawLine(new Vector3(GetPosX(0), GetPosY(0)), new Vector3(GetPosX(0), GetPosY(max)));// 竖线
            Handles.Label(new Vector3(1, GetPosY(0)), "0");
            Handles.Label(new Vector3(d_wid - 10, GetPosY(0)), maxTime.ToString("0.0"));
            // 绘制原曲线
            Handles.color = Color.green;
            Handles.DrawLine(new Vector3(startx, GetPosY(1)), new Vector3(d_wid, GetPosY(1)));
            Handles.Label(new Vector3(0, GetPosY(1)), "1");

            // 绘制经过处理的曲线数据
            for (int i = 0; i < listY.Count - 1; i++)
            {
                Vector3 start = new Vector3();
                start.x = GetPosX(i * delTime);
                start.y = GetPosY(listY[i]);

                Vector3 end = new Vector3();
                end.x = GetPosX((i + 1) * delTime);
                end.y = GetPosY(listY[i + 1]);

                Handles.DrawLine(start, end);
            }

            Handles.EndGUI();
        }

        // ________ | max > 0
        //          |          |
        //          | 设置高度 | 计算用的高度
        // ________ |  0       |
        //          |
        // ________ | min < 0

        /// <summary> 给定y 输出 posY，y范围（-max，max），max>0 </summary>
        private float GetPosY(float datay)
        {
            return starty + (1f - datay / max) * high;// 起始y + datay换算的百分比 * 曲线图高度
        }

        /// <summary> 给定x 输出 posX，x范围 （0f，2f） </summary>
        private float GetPosX(float datax)
        {
            return startx + datax / maxTime * d_wid;
        }
    }

}