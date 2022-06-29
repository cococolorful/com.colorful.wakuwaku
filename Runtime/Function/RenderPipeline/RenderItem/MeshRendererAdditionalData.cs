using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class MeshRendererAdditionalData : MonoBehaviour
    {
        // Start is called before the first frame update
        MeshRenderer mesh_renderer_;
        MeshFilter mesh_filter_;

        public MaterialPropertyBlock materialPropertyBlock;

        void Start()
        {
            mesh_renderer_ = GetComponent<MeshRenderer>();
            mesh_filter_ = GetComponent<MeshFilter>();
        }

        private void OnEnable()
        {
            
        }
        
        
        // Update is called once per frame
        void Update()
        {
            if(RenderSceneManager.Instance != null)
                RenderSceneManager.Instance.SceneBVH.UpdateInstanceTransform(mesh_renderer_);
        }
    }

}
