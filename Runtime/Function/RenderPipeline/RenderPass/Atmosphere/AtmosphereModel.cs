
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    // um-1
    // reference <<A Scalable and Production Ready Sky and Atmosphere Rendering Technique>>
    public struct AtmosphereModel
    {
        public Vector3 scatterRayleigh;
        public float hDensityRayleigh;

        public float scatterMie;
        public float asymmetryMie;
        public float absorbMie;
        public float hDensityMie;

        public Vector3 absorbOzone;
        public float ozoneCenterHeight;

        public float ozoneThickness;
        public float bottomRadius;
        public float topRadius;
        private float _pad0;

        public AtmosphereModel GetStandardUnit()
        {
            AtmosphereModel atmosphereModel = new AtmosphereModel();

            atmosphereModel.scatterRayleigh = 1e-6f * scatterRayleigh;
            atmosphereModel.hDensityRayleigh = 1e3f * hDensityRayleigh;
            atmosphereModel.scatterMie = 1e-6f * scatterMie;
            atmosphereModel.absorbMie = 1e-6f * absorbMie;
            atmosphereModel.hDensityMie = 1e3f * hDensityMie;
            atmosphereModel.absorbOzone = 1e-6f * absorbOzone;
            atmosphereModel.ozoneCenterHeight = 1e3f * ozoneCenterHeight;
            atmosphereModel.ozoneThickness = 1e3f * ozoneThickness;
            atmosphereModel.bottomRadius = 1e3f * bottomRadius;
            atmosphereModel.topRadius = 1e3f * topRadius;
            return atmosphereModel;
        }
    }

    public class AtmosphereResourece : IRenderPass<AtmosphereResourece>
    {
        [PortPin(PinType.Write)]
        public ComputeBufferHandle atmosphere;

        public Vector3 scatterRayleigh = new Vector3(5.802f, 13.558f, 33.1f); //1m
        public float hDensityRayleigh = 8;

        public float scatterMie = 3.996f;
        public float asymmetryMie = 0.8f;
        public float absorbMie = 4.4f;
        public float hDensityMie = 1.2f;

        public Vector3 absorbOzone = new Vector3(0.65f, 1.881f, 0.085f); // 1m
        public float ozoneCenterHeight = 25;

        public float ozoneThickness = 30;
        public float bottomRadius = 6360;
        public float topRadius = 6460;

        List<AtmosphereModel> atmosphereInfo = new List<AtmosphereModel>(1);

        protected override void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset)
        {
            //atmosphereInfo = new List<AtmosphereModel>(1);
            AtmosphereModel atmosphereModel = new AtmosphereModel();

            //             atmosphereModel.scatterRayleigh = 1e-6f * scatterRayleigh;
            //             atmosphereModel.hDensityRayleigh = 1e3f * hDensityRayleigh;
            //             atmosphereModel.scatterMie = 1e-6f * scatterMie;
            //             atmosphereModel.absorbMie = 1e-6f * absorbMie;
            //             atmosphereModel.hDensityMie = 1e3f * hDensityMie;
            //             atmosphereModel.absorbOzone = 1e-6f * absorbOzone;
            //             atmosphereModel.ozoneCenterHeight = 1e3f * ozoneCenterHeight;
            //             atmosphereModel.ozoneThickness = 1e3f * ozoneThickness;
            //             atmosphereModel.bottomRadius = 1e3f * bottomRadius;
            //             atmosphereModel.topRadius = 1e3f * topRadius;
            atmosphereModel.scatterRayleigh = 1e-3f * scatterRayleigh;
            atmosphereModel.hDensityRayleigh =  hDensityRayleigh;
            atmosphereModel.scatterMie = 1e-3f * scatterMie;
            atmosphereModel.absorbMie = 1e-3f * absorbMie;
            atmosphereModel.hDensityMie =  hDensityMie;
            atmosphereModel.absorbOzone = 1e-3f * absorbOzone;
            atmosphereModel.ozoneCenterHeight =  ozoneCenterHeight;
            atmosphereModel.ozoneThickness =  ozoneThickness;
            atmosphereModel.bottomRadius =  bottomRadius;
            atmosphereModel.topRadius =  topRadius;

            atmosphereInfo.Clear();
            atmosphereInfo.Add(atmosphereModel);

            atmosphere = CreateComputeBuffer(new ComputeBufferDesc() { count = 1, name = nameof(atmosphere), stride = Marshal.SizeOf<AtmosphereModel>(), type = ComputeBufferType.Structured });
        }

        protected override void Execute(RenderGraphContext ctx)
        {
            
            ctx.cmd.SetBufferData<AtmosphereModel>(atmosphere, atmosphereInfo);
            ctx.cmd.SetGlobalConstantBuffer(atmosphere, "AtmosphereBuffer", 0, Marshal.SizeOf<AtmosphereModel>());
        }
    }
}
