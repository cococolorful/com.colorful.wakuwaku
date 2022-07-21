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
        public Camera m_camera;

        int width,height;
        public bool use_native_render = false;

        public int idx;
        public int frame_buffer_count=2;
        public Texture2D []native_render_texture;
        public WRenderGraph render_graph;
        // Start is called before the first frame update


        [DllImport("NativeRenderer")]
        private static extern void OnSize(int width, int height, int frame_buffer_count, IntPtr[] color_handles);
        [DllImport("NativeRenderer")]
        private static extern void OnNativeRendererQuit();
        [DllImport("NativeRenderer")]
        private static extern void OnInitialize();
        static bool sigle = false ;
        private void Awake()
        {
            OnInitialize();

            Init();
        }
            
        private void OnDisable()
        {
            OnNativeRendererQuit();
        }
        public void Init()
        {
            m_camera = GetComponent<Camera>();
            Modify();
        }
        void Modify()
        {
            width = m_camera.pixelWidth;
            height = m_camera.pixelHeight;

            IntPtr[] color_handles = new IntPtr[frame_buffer_count];

            OnSize(width, height, frame_buffer_count, color_handles);
            native_render_texture = null;
            native_render_texture = new Texture2D[frame_buffer_count];
            for (int i = 0; i < frame_buffer_count; i++)
            {
                native_render_texture[i] = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBAFloat, true, true, color_handles[i]);
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.TextArea("test for funny");
            GUILayout.EndHorizontal();
        }
        // Update is called once per frame

        [DllImport("NativeRenderer")]
        private static extern int Render();

        
        void Update()
        {
            if(width != m_camera.pixelWidth || height != m_camera.pixelHeight)
            {
                Modify();
            }
            int frame_idx = Render();
            Debug.Log(frame_idx);
            idx = frame_idx;
            //Graphics.Blit(native_render_texture[frame_idx], m_camera.targetTexture);
        }
    }

}


