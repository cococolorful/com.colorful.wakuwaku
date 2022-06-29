#ifndef LIGHTS_LIGHT_COMMON_HLSL
#define LIGHTS_LIGHT_COMMON_HLSL
#include "../Util/Spectrum.hlsl"
#include "../RTCommon.hlsl"
#include "../SampleTransformUtil.hlsl"

struct PathTracingLightOut
{
    float scale; //defalut  1
    float temperature;
    
    float3 Position;
    float3 Normal;
    
    float3 Dimensions;
    float Attenuation;
    float FalloffExponent;
    float RectLightBarnCosAngle;
    float RectLightBarnLength;
    int IESTextureSlice;
    int Flags;

    //float Padding[8];
    
    bool IsSpotLight() // Mesh light
    {
        return Flags == 0;
    }
    bool IsDistantLight() // Directional light
    {
        return Flags == 1;
    }
    bool IsPointLight()
    {
        return Flags == 2;
    }
    bool IsDiffuseAreaLight() // Mesh light
    {
        return Flags == 3;
    }
    
    //float3 GetIntensity()
    //{
    //    return Color;
    //    return IsPointLight() ? Color : 0;
    //}
    //float3 GetRadiance()
    //{
    //    return Color;
    //    return IsPointLight() ? 0 : Color;
    //}

    //LightSample _PointLight_SampleLight()
    //{
    //    LightSample res = (LightSample) 0;
    //    if (!IsPointLight())
    //    {
    //        return res;
    //    }
    //
    //    res.position = Position;
    //    // res.normal
    //    res.radiance
    //}
};


struct PathTracingLight
{
    int shape_id;
    float3 radiance_or_irradiance; 
    float area;
    int to_world_id;
};

struct LightLiSample
{
    Spectrum radiance;
    float pdf;
    
    float3 position;
    float3 normal;
    
    float3 to_light;
};
struct EmissiveVertex
{
    float3 position;
    float3 normal;
};

StructuredBuffer<uint3> g_compressed_index_buffer;
StructuredBuffer<EmissiveVertex> g_compressed_vertex_buffer;

StructuredBuffer<float4x4> g_compressed_to_world_buffer;

StructuredBuffer<PathTracingLight> g_light_buffer;
//float3 GetLightPosition(int LightId)
//{
//    return _Light[LightId].Position;
//}
//
//float3 GetLightNormal(int LightId)
//{
//    return _Light[LightId].Normal;
//}
//float3 GetLightRadiance(int LightId)
//{
//    return _Light[LightId].GetRadiance();
//}
//float3 GetLightIntensity(int LightId)
//{
//    return _Light[LightId].GetIntensity();
//}
#endif