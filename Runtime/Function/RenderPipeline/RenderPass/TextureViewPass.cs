
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    public class TextureViewPass : IRenderPass<TextureViewPass> 
    {
        [PortPin(PinType.ReadWrite)]
        public TextureHandle src;
        
        public RenderTexture texture;

        public Material blit;
        protected override void Execute(RenderGraphContext ctx)
        {
            RenderTexture s = src;

            if (s.width != texture.width || s.height != texture.height )
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = RenderTexture.GetTemporary(s.width,s.height,0,RenderTextureFormat.ARGB32);
            }
            if(blit==null)
                ctx.cmd.Blit(src, texture);
            else
                ctx.cmd.Blit(src, texture,blit);
        }



        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            if (texture == null)
                texture = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight,0);
            if (blit == null)
                blit = (asset as WRenderPipelineAsset).BlitMat;
        }

        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(texture);
        }
    }

}
