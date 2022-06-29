#ifndef MATERIAL_CONDUCTOR_HLSL
#define MATERIAL_CONDUCTOR_HLSL

#include "MaterialCommon.hlsl"

BSDFSample Conductor_SampleMaterial(MaterialClosestHitPayload payload, float3 woRender, inout RandomSequence rndSequence)
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
BSDFEval Conductor_EvalMaterial(
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


#include "../RTCommon.hlsl"

#include "MaterialCommon.hlsl"

#include "../Lights/LightSampling.hlsl"
#include "../SceneUtil.hlsl"
#include "../../Input.hlsl"

cbuffer UnityPerMaterial
{
    float eta[NSpectrumSamples];
    float k[NSpectrumSamples];
};
[shader("closesthit")]
void ClosestHitShader(inout PathVertexPayload vertexPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    if (vertexPayload.IsNormalTest())
    {
        NormalTexCoord shadingPoint = GetShadingPoint(attributeData);
        
        float3 Metallic = spec.b;
        float3 Roughness = spec.g;
        float3 WorldNormal = normalWS;
        float3 Radiance = _EmissiveFactor * _EmissiveTex.SampleLevel(GetLinearClampSampler(), texCoord, 0).xyz;
                
        BSDFSample result;
        float2 u = RandomSequence_GenerateSample2D(vertexPayload.RndSequence);
        float a = 1.0 - 2.0 * u.x;
        float b = sqrt(1 - a * a);
        float phi = 2 * M_PI * u.y;
        result.wiRender = normalize(WorldNormal + float3(b * cos(phi), b * sin(phi), a));
        
        result.f = BaseColor.xyz * InvPi;
        result.pdf = SafeDot(result.wiRender, WorldNormal) * InvPi;
        
        vertexPayload.Emissive = Radiance;
        vertexPayload.Position = WorldPosition;
        vertexPayload.Normal = WorldNormal;
        vertexPayload.BRDFSamplingResult.f = result.f;
        vertexPayload.BRDFSamplingResult.pdf = result.pdf;
        vertexPayload.BRDFSamplingResult.wiRender = result.wiRender;
        vertexPayload.DirectLightEstimation = 0; // direct light sampling
        
        vertexPayload.Radiance.Fill(Radiance.x);
        
        LightSample sampleRes = SampleLights(vertexPayload.RndSequence);
            
        if (sampleRes.pdf > 0)
        {
            LightLiSample lightLiSampleRes = SampleLi(WorldPosition, WorldNormal, sampleRes.lightID, vertexPayload.RndSequence);
            lightLiSampleRes.pdf *= sampleRes.pdf;
                    
            if (lightLiSampleRes.pdf > 0)
            {
                float3 wiRender = normalize(lightLiSampleRes.position - WorldPosition);
                BSDFEval bsdf;
                float3 NorWorld = WorldNormal;
        
                bsdf.f = BaseColor.xyz * InvPi;
                float NoL = saturate(dot(NorWorld, wiRender));
        
                bsdf.pdf = InvPi * NoL;
                    
                // float GTerm = SafeDot(wiRender, WorldNormal) * SafeDot(-wiRender, lightLiSampleRes.normal) / Pow2(length(lightLiSampleRes.position - WorldPosition));
                       
                //RayDesc directRay = UpdateRay(WorldPosition, WorldNormal, wiRender);
                //directRay.TMax = length(lightLiSampleRes.position - WorldPosition);
                {
                    //vertexPayload.DirectLightEstimation = float3(TraceVisibilityRay(directRay), TraceVisibilityRay(directRay), TraceVisibilityRay(directRay));
                    //vertexPayload.DirectLightEstimation = wiRender;
                    //return;
                    vertexPayload.DirectLightEstimation = (TraceVisibilityRay(WorldPosition, WorldNormal, wiRender, length(lightLiSampleRes.position - WorldPosition)) * lightLiSampleRes.radiance * bsdf.f * SafeDot(wiRender, WorldNormal) / lightLiSampleRes.pdf);
                }
            }
        }
    }
   else if (vertexPayload.IsVisibility())
   {
        //vertexPayload.SetMiss();

    }

}
             
#endif