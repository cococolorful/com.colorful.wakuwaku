using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wakuwaku.Function.WRenderPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class WakuAdditionalCameraData : MonoBehaviour
    {
        Camera m_camera;

        public WRenderGraph render_graph;
        // Start is called before the first frame update
        void Start()
        {
            m_camera = GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}


