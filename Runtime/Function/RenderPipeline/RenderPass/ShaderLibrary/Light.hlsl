#ifndef LIGHT_HLSL
#define LIGHT_HLSL
#include"../ShaderLibrary/MathUtil.hlsl"

struct LightLiSample
{
    float3 radiance;
    float3 wi;
    float pdf;

    // intersection    
    float3 position;
    float3 normal;
   
};
struct LightSampleContext
{
    float3 position;
    float3 normal;
};
struct DistantLight
{
    float3 radiance_emit;
    float scale;
    
    float3 scene_center;
    float scene_radius;
    
    float4x4 local_to_world;
  
    LightLiSample SampleLi(const LightSampleContext ctx)
    {
        LightLiSample s;
        s.wi = normalize(mul(local_to_world, float4(0, 0, 1, 0)).xyz);        
        s.radiance = scale * radiance_emit;
        s.pdf = 1;
        s.position = ctx.position + s.wi * (2 * scene_radius);
        
        return s;
    }
};
struct PointLight
{
    float3 intensity;
    float scale;
    
    float4x4 local_to_world;
    
    LightLiSample SampleLi(const LightSampleContext ctx, float2 u2)
    {
        LightLiSample s;
        s.position = mul(local_to_world, float4(0, 0, 0, 1)).xyz;
        s.wi = normalize(s.position - ctx.position);
        s.radiance = scale * intensity / Pow2(length(s.position - ctx.position));
        s.pdf = 1;
        
        return s;
    }
};

cbuffer LightConfig
{
    int g_point_light_count;
    int g_distant_light_count;
    int g_area_light_count;
}

//StructuredBuffer<float4x4> g_light_buffer_transform;
StructuredBuffer<PointLight> g_light_buffer_point;
StructuredBuffer<DistantLight> g_light_buffer_distant;



class MetalWorkflowBRDF;
float3 SampleLd(const LightSampleContext ctx, MetalWorkflowBRDF s)
{
    return float3(0, 0, 1);

}
#endif