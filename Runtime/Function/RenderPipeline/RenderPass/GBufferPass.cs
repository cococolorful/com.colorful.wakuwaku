
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace wakuwaku.Function.WRenderPipeline
{

    public class GBufferPass : IRenderPass<GBufferPass>
    {
        Material    skyBox ;
        ShaderTagId passId ;

        [PortPin(PinType.Write)]
        public TextureHandle depthBuffer;
        [PortPin(PinType.Write)]
        public TextureHandle gBufferWSPos; // lit from environment
        [PortPin(PinType.Write)]
        public TextureHandle gBufferWSNorm;
        [PortPin(PinType.Write)]
        public TextureHandle gBufferMatDif;
        [PortPin(PinType.Write)]
        public TextureHandle gBufferMatSpec;
        [PortPin(PinType.Write)]
        public TextureHandle gBufferLinearZAndNormal;
        [PortPin(PinType.Write)]
        public TextureHandle gBufferMotionVecFwidth;
        [PortPin(PinType.Write)]
        public RendererListHandle rendererList;

        public enum E
        {
            a, b, c, d
        }

        public float a = 1;
        public bool b = false;
        public Vector2 c = new Vector2(1.10973f, 2.0f);
        public E d = E.d;
        public LayerMask e = 10;
        public Color f = new Color(1, 2, 3, 0.22334f);

        private Matrix4x4 view, proj, previousViewProjectionMatrix;
        protected override void Execute(
            RenderGraphContext ctx)
        {
            var cmd = ctx.cmd;
            cmd.SetRenderTarget(new RenderTargetIdentifier[] {
                            gBufferWSPos,
                            gBufferWSNorm,
                            gBufferMatDif,
                            gBufferMatSpec ,
                            gBufferLinearZAndNormal,
                            gBufferMotionVecFwidth}, depthBuffer);
            cmd.ClearRenderTarget(true, true, Color.black);

            cmd.SetGlobalMatrix("_Last_PV_NoJitter", previousViewProjectionMatrix);
            cmd.SetViewProjectionMatrices(view, proj);
            //cmd.DrawProcedural(Matrix4x4.identity, skyBox, 0, MeshTopology.Triangles, 36);
            //cmd.RasterizeScene("GBufferPass");
            //cmd.DrawRenderer()
            cmd.DrawRendererList(rendererList);
        }
        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            
            //skyBox = (asset as BRenderPipelineAsset).skyBox;
            //skyBox.SetTexture("_SkyboxTex", (asset as BRenderPipelineAsset).SkyBoxTex);


            previousViewProjectionMatrix = camera.previousViewProjectionMatrix;            
            view = camera.worldToCameraMatrix;
            proj = camera.projectionMatrix;
            gBufferWSNorm = CreateTexture
                (new TextureDesc(camera.pixelWidth, camera.pixelHeight)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                    name = "gBufferWSNorm",
                    clearBuffer = true,
                    clearColor = Color.clear
                });
            gBufferWSPos = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = "gBufferWSPos",
                clearBuffer = true,
                clearColor = Color.black
            });
            gBufferMatDif = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = "gBufferMatDif",
                clearBuffer = true,
                clearColor = Color.clear
            });
            gBufferMatSpec = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = "gBufferMatSpec",
                clearBuffer = true,
                clearColor = Color.clear
            }); 
            gBufferLinearZAndNormal = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = "gBufferLinearZAndNormal",
                clearBuffer = true,
                clearColor = Color.clear
            });
            gBufferMotionVecFwidth = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = "gBufferMotionVecFwidth",
                clearBuffer = true,
                clearColor = Color.clear
            });
            depthBuffer = CreateTexture(new TextureDesc(camera.pixelWidth, camera.pixelHeight)
            {
                depthBufferBits = DepthBits.Depth32,
                name = "depthBuffer",
                clearBuffer = true,
            });

            camera.TryGetCullingParameters(out var cullingParameters);
            var cullResults = context.Cull(ref cullingParameters);
            passId = new ShaderTagId(nameof(GBufferPass));
            RendererListDesc gbuffer = new RendererListDesc(passId, cullResults, camera);
            gbuffer.renderQueueRange = RenderQueueRange.all;
           
            gbuffer.sortingCriteria = SortingCriteria.None;
            rendererList = CreateRendererList(gbuffer);
        }
    }

}