

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

        

        Material material;
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
            var passId = new ShaderTagId(nameof(GBufferPass));
            //cmd.RasterizeScene(0);
            //cmd.DrawMeshInstancedIndirect(,,,,,)
            //Scene.Instance.Rasterize(material);
            
        }
        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            material = new Material(Shader.Find("wakuwaku/Hidden/GBufferRaster"));


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
        }
        public void Dispose()
        {

        }
    }

}