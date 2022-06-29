#ifndef DIFFUSE_HLSL
#define DIFFUSE_HLSL

#include "../SampleTransformUtil.hlsl"
#include "MaterialCommon.hlsl"
class DiffuseBXDF
{
    Spectrum reflectance_;
    
    void Init(in Spectrum reflectance)
    {
        reflectance_ = reflectance;
    }
    
    BSDFSample Sample(float3 wo, float2 u)
    {
        BSDFSample result;
        
        result.wi = SampleCosineHemisphere(u);
        result.pdf = CosineHemispherePDF(AbsCosineTheta(result.wi));
       
        result.f = reflectance_ * InvPi;
        return result;
    }
    Spectrum Eval(float3 wo, float3 wi)
    {
        if (!IsSameHemisphere(wi,wo))
        {
            return (Spectrum) 0;
        }
        return reflectance_ * InvPi;
    }
    float PDF(float3 wo, float3 wi)
    {
        return CosineHemispherePDF(AbsCosineTheta(wi));
    }
};


#include "../RTCommon.hlsl"

#include "MaterialCommon.hlsl"

#include "../Lights/LightSampling.hlsl"
#include "../SceneUtil.hlsl"
#include "../../Input.hlsl"
            //#include "../ShaderLibrary/SampleTransformUtil.hlsl"
#include "../Unity/UnityRaytracingMeshUtils.cginc"
            

//  Texture2D _NormalMap;
//              
Texture2D _EmissiveTex;
float3 _EmissiveFactor;

Texture2D _AlbedoTex;
float3 _AlbedoFactor;
float4 _AlbedoTex_ST;
[shader("closesthit")]
void ClosestHitShader(inout PathVertexPayload vertexPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    if (vertexPayload.IsNormalTest())
    {
        NormalTexCoord shadingPoint = GetShadingPoint(attributeData);
        
        vertexPayload.radiance_ = _EmissiveFactor * _EmissiveTex.SampleLevel(GetLinearClampSampler(), shadingPoint.texCoord, 0).xyz;
        
        float pdf_light_sampling_in_area = 1.0 / shadingPoint.area;
        float g_item = SafeDot(-vertexPayload.BRDFSamplingResult.wi, shadingPoint.shadingFrame.Normal()) / Pow2(RayTCurrent());
        vertexPayload.area_light_pdf_in_solid_ = pdf_light_sampling_in_area / g_item;
        
        DiffuseBXDF currentBXDF;
        {
            Spectrum albedo = _AlbedoFactor * _AlbedoTex.SampleLevel(GetLinearClampSampler(), shadingPoint.texCoord * _AlbedoTex_ST.xy + _AlbedoTex_ST.zw, 0).xyz;
            currentBXDF.Init(albedo);
        }
        //if (dot(shadingPoint.shadingFrame.Normal(), -WorldRayDirection()) < 0)
        //    shadingPoint.shadingFrame.Filp();
        
        vertexPayload.SetShadingPoint(shadingPoint);
        
        //vertexPayload.BRDFSamplingResult.pdf *= g_item;
        //float wight_brdf_sample = PowerHeuristic(1, vertexPayload.BRDFSamplingResult.pdf, 1, pdf_light_sampling); // = vertexPayload.BRDFSamplingResult.pdf / (vertexPayload.BRDFSamplingResult.pdf + pdf_light_sampling);
        //// phslight
        //vertexPayload.DirectLightEstimation = wight_brdf_sample * _EmissiveFactor * _EmissiveTex.SampleLevel(GetLinearClampSampler(), shadingPoint.texCoord, 0).xyz;;
        
        float3 wo = shadingPoint.shadingFrame.ToLocal(-WorldRayDirection());
        vertexPayload.BRDFSamplingResult = currentBXDF.Sample(wo, RandomSequence_GenerateSample2D(vertexPayload.RndSequence));
        //shadingPoint.shadingFrame.Filp();
        vertexPayload.BRDFSamplingResult.wi = normalize(shadingPoint.shadingFrame.ToWorld(vertexPayload.BRDFSamplingResult.wi));
    }
   else if (vertexPayload.IsVisibility())
   {
        if (abs(vertexPayload.t - RayTCurrent()) < 1e-3f)
        vertexPayload.SetMiss();
    }

}
             
#endif