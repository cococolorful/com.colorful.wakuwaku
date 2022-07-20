using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace wakuwaku.Function.WRenderPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class WakuAdditionalCameraData : MonoBehaviour
    {
        Camera m_camera;

        int width,height;

        public int frame_buffer_count;
        public Texture2D []native_render_texture;
        public WRenderGraph render_graph;
        // Start is called before the first frame update
        void Start()
        {
            m_camera = GetComponent<Camera>();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.TextArea("test for funny");
            GUILayout.EndHorizontal();
        }
        // Update is called once per frame

        [DllImport("NativeRenderer")]
        private static extern void OnSize(int width,int height,int frame_buffer_count,IntPtr[] color_handles);
        void Update()
        {
            if(width != m_camera.pixelWidth || height != m_camera.pixelHeight)
            {
                width = m_camera.pixelWidth;
                height = m_camera.pixelHeight;

                IntPtr[] color_handles = new IntPtr[frame_buffer_count];
                
                OnSize(width, height, frame_buffer_count, color_handles);
                for (int i = 0; i < frame_buffer_count; i++)
                {
                    Debug.Log(color_handles[i]);// = new IntPtr();
                }
            }
        }
    }

}


