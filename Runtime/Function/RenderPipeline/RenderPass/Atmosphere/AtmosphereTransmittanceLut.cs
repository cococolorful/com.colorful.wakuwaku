
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    public class AtmosphereTransmittanceLut : IRenderPass<AtmosphereTransmittanceLut>
    {
        [PortPin(PinType.Read)]
        public ComputeBufferHandle AtmosphereModel;

        [PortPin(PinType.Write)]
        public TextureHandle transmittanceLut;

        public ComputeShader transLutCS;
        public int InterSample = 200;
        public Vector2 TransLutSize = new Vector2(256, 64);
        protected override void Execute(RenderGraphContext ctx)
        {
            int kernel = transLutCS.FindKernel("AtmosphereTransmittanceLut");

            var cb = ctx.cmd;
            cb.SetComputeTextureParam(transLutCS, kernel, "Output", transmittanceLut);

            cb.SetComputeIntParam(transLutCS, "RayMatchSample", InterSample);

            const int threadSize = 8;
            int threadX = Mathf.CeilToInt(TransLutSize.x / threadSize);
            int threadY = Mathf.CeilToInt(TransLutSize.y / threadSize);
            cb.DispatchCompute(transLutCS,kernel,threadX, threadY, 1);
        }


        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            transmittanceLut = CreateTexture(new TextureDesc(TransLutSize)
            {
                colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                name = nameof(transmittanceLut),
                enableRandomWrite = true
            });
            //transLutCS = (asset as BRenderPipelineAsset).transmitLutCS;
        }
    }
}