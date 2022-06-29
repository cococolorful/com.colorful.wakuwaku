

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    public class AtmosphereMutiScatter : IRenderPass<AtmosphereMutiScatter>
    {
        

        [PortPin(PinType.Read)]
        public TextureHandle transmittanceLut;


        [PortPin(PinType.Write)]
        public TextureHandle mutiScatterLut;

        [PortPin(PinType.Write)]
        public TextureHandle skyViewLut;

        public ComputeShader mutiScatterLutCS;
        public int InterSample = 20;
        public Vector2 mutiScatterLutSize = new Vector2(32, 32);
        public Vector2 skyViewLutSize = new Vector2(200, 100);

        public float Intensity = 1.0f;
        protected override void Execute(RenderGraphContext ctx)
        {
             int kernel = mutiScatterLutCS.FindKernel("mutiScatterLutCS");
 
             var cb = ctx.cmd;
            cb.SetComputeTextureParam(mutiScatterLutCS, kernel, "TransmittanceLut", transmittanceLut);
            cb.SetComputeTextureParam(mutiScatterLutCS, kernel, "MutiScatteringLut", mutiScatterLut);
            cb.SetComputeIntParam(mutiScatterLutCS, "RayMatchSample", InterSample);
            cb.SetGlobalVector("SunIntensity", Vector4.one * Intensity);
            
            cb.DispatchCompute(mutiScatterLutCS, kernel,(int) mutiScatterLutSize.x, (int)mutiScatterLutSize.y, 1);
        }


        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            mutiScatterLut = CreateTexture(new TextureDesc(mutiScatterLutSize)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                name = nameof(mutiScatterLut),
                enableRandomWrite = true
            });
            skyViewLut = CreateTexture(new TextureDesc(skyViewLutSize)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                name = nameof(mutiScatterLut),
                enableRandomWrite = true
            });
        }
    }
}