
using wakuwaku.Function.WRenderPipeline;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace wakuwaku.Function.WRenderPipeline
{

    [CreateAssetMenu(menuName = "wakuwaku/RenderPieplineAsset")]
    public class WRenderPipelineAsset : RenderPipelineAsset
    {
        public WRenderGraph default_render_graph;

        // environmental
        public Texture sky_box_tex;
        public ComputeBuffer sky_box_pdf_;
        public ComputeBuffer sky_box_sampling_prob_;
        public ComputeBuffer sky_box_sampling_alias_;

        
        public ComputeShader transmitLutCS;
        public ComputeShader bitonicSort;


        public Material skyBox;// { get { if (_skyBox == null) _skyBox = new Material(Shader.Find("BRPipeline/Skybox")); return _skyBox; }   }
        private Material _skyBox;

        public Material BlitMat;
        protected override RenderPipeline CreatePipeline()
        {
            //InitializeNaivePlugin();
            Debug.Log("Create Pipeline");
           // Scene.Instance.BuildScene();

            if (Shader.GetGlobalTexture("g_sky_box") == null && sky_box_tex != null)
            {
                EnvironmentalUtil.DealSkyBox(this, sky_box_tex);
            }
            return new WRenderPipeline(this);
        }


        delegate void Log(IntPtr message,int size);

        [DllImport("NativeRenderer")]
        private static extern void RegisterLog(Log log_info, Log log_warning, Log log_error);
        private void InitializeNaivePlugin()
        {
            RegisterLog(LogInfoMessageFromCpp,LogWarningMessageFromCpp,LogErrorMessageFromCpp);                                                                                                         
            //RegisterLog(Debug.Log,Debug.LogWarning,Debug.LogError);
        }

        public static void LogInfoMessageFromCpp(IntPtr message,int size)
        {
            Debug.Log(Marshal.PtrToStringAnsi(message, size));
        }
        public static void LogWarningMessageFromCpp(IntPtr message, int size)
        {
            Debug.LogWarning(Marshal.PtrToStringAnsi(message, size));
        }
        public static void LogErrorMessageFromCpp(IntPtr message, int size)
        {
            Debug.LogError(Marshal.PtrToStringAnsi(message, size));
        }
        private void OnDisable()
        {
            //Scene.Clear();
            Debug.Log("OnDisable Pipeline");
        }

    }

    public unsafe class WRenderPipeline : RenderPipeline
    {
        WRenderPipelineAsset mAsset;

        
        public static ScriptableRenderContext context;
        public WRenderPipeline(WRenderPipelineAsset bRenderPipelineAsset)
        {
            mAsset = bRenderPipelineAsset;

        }
        protected override void Dispose(bool disposing)
        {
            //             void Dispose(WRenderGraph obj)
            //             {
            //                 if (obj != null)
            //                     obj.Cleanup();
            //                 obj = null;
            //             }
            //             base.Dispose(disposing);
            //             Dispose(mGameRenderGraph);
            //             Dispose(mSceneRenderGraph);
        }

        public struct CameraData
        {
            public Matrix4x4 view_matrix;
            public Matrix4x4 prev_view_matrix;
            public Matrix4x4 proj_matrix;
            public Matrix4x4 view_proj_matrix;
            public Matrix4x4 inv_view_proj_matrix;
            public Matrix4x4 view_proj_no_jitter_matrix;
            public Matrix4x4 prev_view_proj_no_jittermatrix;
            public Matrix4x4 proj_no_jitter_matrix;


            public Vector3 pos_world;
            public float near_z;

            public Vector3 right;
            public float jitter_x;

            public Vector3 up;
            public float jitter_y;

            public Vector3 forward;
            public float far_z;
        };
        public void BindCamera(Camera camera)
        {
            camera_data.view_matrix = camera.worldToCameraMatrix;
            camera_data.proj_matrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix,true);
            Debug.Log(camera_data.proj_matrix);
            camera_data.view_proj_matrix = camera_data.view_matrix * camera_data.proj_matrix;

            ApplyCamera(UnsafeUtility.AddressOf(ref camera_data));
        }

        CameraData camera_data = new CameraData();
        [DllImport("NativeRenderer")]
        private static extern void ApplyCamera(void* camera_data);
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            WRenderPipeline.context = context;

            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras)
            {
                camera.TryGetComponent<WakuAdditionalCameraData>(out var wakuAdditionalCameraData);

                if (Application.isPlaying && wakuAdditionalCameraData != null && wakuAdditionalCameraData.use_native_render == true)
                {
                    BindCamera(camera);
                    BeginCameraRendering(context, camera);
                    try
                    {
                        Graphics.DrawTexture(new Rect(0, 0, camera.pixelWidth, camera.pixelHeight), wakuAdditionalCameraData.native_render_texture[wakuAdditionalCameraData.idx]);
                    }
                    catch (Exception e)
                    {
                        wakuAdditionalCameraData.Init();
                    }

                    EndCameraRendering(context, camera);
                    continue;
                }
                
                WRenderGraph rg = GetCameraRenderGraph(camera);

                BeginCameraRendering(context, camera);

                rg.Excute(context, camera, mAsset);

                EndCameraRendering(context, camera);

                rg.EndFrame();
            }


            EndFrameRendering(context, cameras);
        }

        private WRenderGraph GetCameraRenderGraph(Camera camera)
        {
            WRenderGraph rg;

            camera.TryGetComponent<WakuAdditionalCameraData>(out var additional_data);
            if (additional_data == null || additional_data.render_graph == null)
                rg = mAsset.default_render_graph;
            else
                rg = additional_data.render_graph;

            if (rg == null)
                Debug.LogWarning("the default render graph is null");
            return rg;
        }
    }


}
