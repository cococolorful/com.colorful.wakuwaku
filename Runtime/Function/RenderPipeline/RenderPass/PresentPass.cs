
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    public class PresentPass : IRenderPass<PresentPass>
    {
        [PortPin(PinType.Read)]
        public TextureHandle src;

        [PortPin(PinType.Write)]
        public TextureHandle bgColor;

        public Material blit;
        protected override void Execute(RenderGraphContext ctx)
        {
            
            var cmd = ctx.cmd;
            cmd.Blit(src, bgColor, blit);

            //             var kernel = Blit.FindKernel("Blit");
            //             //ctx.cmd.Blit(PresentPassData.src, presentData.bgColor, new Vector2(1, -1), new Vector2(0, 1));
            //             cmd.SetComputeTextureParam(Blit, kernel, "Src", src);
            //             cmd.SetComputeTextureParam(Blit, kernel, "Result", bgColor);
            //             cmd.DispatchCompute
        }

       

        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            bgColor = ImportBackbuffer(camera.targetTexture);
        }
    }
   
}
