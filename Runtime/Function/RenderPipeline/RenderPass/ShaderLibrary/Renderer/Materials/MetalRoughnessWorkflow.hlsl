#ifndef METAL_ROUGHNESS_WORKFLOW_HLSL
#define METAL_ROUGHNESS_WORKFLOW_HLSL
#define  FORCE_DIFFUSE

#include "MaterialCommon.hlsl"

BSDFSample MetalRoughness_SampleMaterial(MaterialClosestHitPayload payload, float3 woRender, inout RandomSequence rndSequence)
{
    BSDFSample result;
    // float3 outDir = CosineSampleHemisphere(rndSequence);
    // result.wiRender = normalize(TangentToWorld(outDir, payload.GetWorldNormal()));
    
    //result.wiRender = payload.GetWorldNormal();
    float2 u = RandomSequence_GenerateSample2D(rndSequence);
    float a = 1.0 - 2.0 * u.x;
    float b = sqrt(1 - a * a);
    float phi = 2 * M_PI * u.y;
    result.wiRender = normalize(payload.GetWorldNormal() + float3(b * cos(phi), b * sin(phi), a));
    
    result.f = payload.GetBaseColor() * InvPi;
    //result.f =  InvPi;
    result.pdf = SafeDot(result.wiRender, payload.GetWorldNormal()) * InvPi;
    
    return result;
}
BSDFEval MetalRoughness_EvalMaterial(
MaterialClosestHitPayload payload,
float3 outDirection
)
{
    
    BSDFEval result;
    
    float3 NorWorld = payload.GetWorldNormal();
    
    result.f = payload.GetBaseColor();
    float NoL = saturate(dot(NorWorld, outDirection));
    
    result.pdf = InvPi * NoL;
    return result;
}

//BSDFEval Lambert_EvalMaterial(
//MaterialClosestHitPayload payload,
//float3 wiRender,float3 woRender
//)
//{
//    float3 VWorld = woRender;
//    float3 LWorld = wiRender;
//    float3 NWorld = payload.GetWorldNormal();
//    
//    
//
//}

//BSDFSample DefaultLit_SampleMaterial(MaterialClosestHitPayload payload, float3 woRender, inout RandomSequence rndSequence)
//{
//    float3 N = payload.GetWorldNormal();
//    float3 V = woRender;
//    
//    float Roughness = payload.GetRoughness();
//    float2 
//
//}
#endif