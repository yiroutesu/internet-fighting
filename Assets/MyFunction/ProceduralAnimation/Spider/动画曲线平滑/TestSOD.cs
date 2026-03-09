using GamesMing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamesMing
{
    public class TestSOD : MonoBehaviour
    {
        public Transform tar;
        [Range(0.01f, 6f)]
        public float f;
        public float z;
        public float r;
        private float lf, lz, lr;
        private SecondOrderDynamics_V_6 caculate;

        void Start()
        {
            caculate = new SecondOrderDynamics_V_6();
            caculate.SetInitPos(transform.position);
            tar.position = transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            CheckMovementResponseFactorChange();
            tar.position = caculate.Update(Time.deltaTime, transform.position);
        }

        private void CheckMovementResponseFactorChange()
        {
            if (f != lf || z != lz || r != lr)
            {
                lf = f;
                lz = z;
                lr = r;
                caculate.SetConstants(f, z, r);
            }
        }
    }

}
