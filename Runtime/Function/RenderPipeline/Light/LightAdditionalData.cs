
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public class LightAdditionalData : MonoBehaviour
    {
        public enum LightType
        {
            SpotLight,
            DistantLight,
            PointLight,
            DiffuseAreaLight
        }
        public LightType lightType;

        public enum LightingPrinciple
        {
            thermalRadiationLuminescence,
            electroLuminescence
        }

        public LightingPrinciple lightingPrinciple;

        public enum LightSPD
        {
            blackbody,
            CIE_F1,
            CIE_F2,
            CIE_F3,

        }
        public LightSPD spdType;

        // Start is called before the first frame update
        public float Temperature;
        public float Power; // lumen
        public Light LightData
        {
            get
            {
                if (_LightData == null)
                    _LightData = GetComponent<Light>();
                return _LightData;
            }
        }
        MeshRenderer _LightRenderer;
        Light _LightData;
        private void Awake()
        {
            Debug.Log("Awake ColLight");
        }
        void Start()
        {
            _LightData = GetComponent<Light>();

        }

        Matrix4x4 mPrevious;
        private void Update()
        {
            if (this.transform.worldToLocalMatrix != mPrevious)
            {
                Scene.Instance.BuildLight();
                ReferencePT.reset = true;
            }
            mPrevious = this.transform.worldToLocalMatrix;
        }
        private void OnEnable()
        {
        }

      
    }

}

