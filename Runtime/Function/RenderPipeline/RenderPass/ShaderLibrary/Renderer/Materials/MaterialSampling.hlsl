#ifndef MATERIAL_SAMPLING_HLSL
#define MATERIAL_SAMPLING_HLSL

#include "MaterialCommon.hlsl"
#include "Diffuse.hlsl"
#include "MetalRoughnessWorkflow.hlsl"

BSDFSample SampleMaterial(MaterialClosestHitPayload payload, float3 woRender, inout RandomSequence rndSequence)
{
    if (payload.ShadingModelID == ShadingModel_Diffuse)
        return Diffuse_SampleMaterial(payload, woRender, rndSequence);
    else if (payload.ShadingModelID == ShadingModel_MetalRoughness)
        return MetalRoughness_SampleMaterial(payload, woRender, rndSequence);
    else
        return Diffuse_SampleMaterial(payload, woRender, rndSequence);
}
BSDFEval EvalMaterial(
MaterialClosestHitPayload payload,
float3 outDir)
{
    if (payload.ShadingModelID == ShadingModel_Diffuse)
        return Diffuse_EvalMaterial(payload, outDir);
    else
        return Diffuse_EvalMaterial(payload, outDir);
}
#endif