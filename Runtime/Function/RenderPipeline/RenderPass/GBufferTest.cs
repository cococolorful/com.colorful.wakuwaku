

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace wakuwaku.Function.WRenderPipeline
{

    public class GBufferTest : IRenderPass<GBufferTest>
    {
        [PortPin(PinType.Write)]
        public TextureHandle RTOutput;

        public RenderTexture RTAccumulation;

        public int MaxBounces = 32;

        public RayTracingShader ReferenceShader;
        private int _AccumulatedFrames = 1;
        public int MaxAccumulatedFrames = 1024;
        Matrix4x4 _InvCameraViewProj;
        Vector3 _CamPosW;
        static public bool reset =  false;
        
        private Camera _Cam;

        void CreatePTSpectrum(int width, int height)
        {
            
            try
            {
                RTAccumulation = RenderTexture.GetTemporary(width,height,0,GraphicsFormat.R32G32B32A32_SFloat);
                RTAccumulation.enableRandomWrite = true;
                RTAccumulation.name = "RTAccumulation";
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
            }

        }
        void  OnSize(int width, int height)
        {
            RenderTexture.ReleaseTemporary(RTAccumulation);
            CreatePTSpectrum(width, height);

        }

        protected override void Execute(
            RenderGraphContext ctx)
        {
            if (Input.GetMouseButton(1))
                reset = true;
            var cmd = ctx.cmd;
            if (reset)
            {
                reset = false;
                _AccumulatedFrames = 1;
            }

            RenderTexture s = RTOutput;

            if (s.width != RTAccumulation.width || s.height != RTAccumulation.height)
            {
                OnSize(s.width, s.height);
            }
            cmd.SetRandomWriteTarget(1, RTAccumulation);
            cmd.SetRayTracingTextureParam(ReferenceShader, "_Accumulation", RTAccumulation);
            
            cmd.SetRayTracingAccelerationStructure(ReferenceShader, "_SceneBVH", RenderSceneManager.Instance.SceneBVH);
            cmd.SetRayTracingTextureParam(ReferenceShader, "_RenderTarget", RTOutput);
            
            cmd.SetRayTracingShaderPass(ReferenceShader, "PathTracing");
            cmd.SetRayTracingIntParam(ReferenceShader, "_TemporalSeed", Random.Range(0,int.MaxValue));

            cmd.SetRayTracingIntParam(ReferenceShader, "_MaxBounces", MaxBounces);
            
            cmd.SetRayTracingIntParam(ReferenceShader, "g_max_accumulated_frames", MaxAccumulatedFrames>0? MaxAccumulatedFrames:int.MaxValue);
            cmd.SetRayTracingIntParam(ReferenceShader, "_AccumulatedFrames", _AccumulatedFrames ++);
            cmd.SetRayTracingMatrixParam(ReferenceShader, "_InvCameraViewProj", _InvCameraViewProj);
            cmd.SetRayTracingVectorParam(ReferenceShader, "_CamPosW", _CamPosW);

            //cmd.DrawMeshInstancedIndirect()
            cmd.DispatchRays(ReferenceShader, "VisibleRayGen", (uint)s.width, (uint)s.height, 1);
            
            
        }
        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {

            if (_Cam != camera)
            {
                _Cam = camera;
                reset = true;
                Dispose();
            }

            _InvCameraViewProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix  ;
            _InvCameraViewProj = _InvCameraViewProj.inverse;

            _CamPosW = camera.transform.position;
            // SceneSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
            RTOutput = CreateTexture
                (new TextureDesc(camera.pixelWidth, camera.pixelHeight)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    name = "RTOutput",
                    clearBuffer = false,
                    clearColor = Color.clear,
                    enableRandomWrite = true,
                });
            
            
            if(RTAccumulation == null)
            {
                CreatePTSpectrum(camera.pixelWidth, camera.pixelHeight);
            }
        }
        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(RTAccumulation);

        }
    }

}