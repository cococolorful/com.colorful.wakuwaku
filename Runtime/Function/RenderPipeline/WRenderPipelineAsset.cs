
using wakuwaku.Function.WRenderPipeline;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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
            Debug.Log("Create Pipeline");
            RenderSceneManager.Initialize();

            if (Shader.GetGlobalTexture("g_sky_box") == null && sky_box_tex != null)
            {
                EnvironmentalUtil.DealSkyBox(this, sky_box_tex);
            }
            return new WRenderPipeline(this);
        }

        private void OnDisable()
        {
            RenderSceneManager.Clear();
            Debug.Log("OnDisable Pipeline");
        }

    }

    public class WRenderPipeline : RenderPipeline
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
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            WRenderPipeline.context = context;




            CommandBuffer commandBuffer = new CommandBuffer();

            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras)
            {
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
