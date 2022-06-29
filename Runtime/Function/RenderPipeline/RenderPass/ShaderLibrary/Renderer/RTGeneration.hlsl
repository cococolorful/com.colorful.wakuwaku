#ifndef RAY_TRACING_HLSL
#define RAY_TRACING_HLSL

#include "RTCommon.hlsl"
#include "Sensors/PinholeCamera.hlsl"
#include "Integrators/PathTracer.hlsl"
#include "SceneData.hlsl"
#include "SceneUtil.hlsl"

#include"Util/Spectrum.hlsl"

inline RayDesc GeneratePrimaryRay(inout RandomSequence rndSequence)
{
    float2 pixel = float2(DispatchRaysIndex().xy);
    float2 resolutions = float2(DispatchRaysDimensions().xy);
    
    float2 offset = RandomSequence_GenerateSample2D(rndSequence);
    pixel += lerp(-0.5.xx, 0.5.xx, offset);
    
    float2 uvInNDC = ((pixel + 0.5) / resolutions) * 2.0f - 1.0f;
    
    return GenPinholeCameraRay(uvInNDC);
}

Spectrum RayIntegrator_Li(RayDesc ray, inout RandomSequence rndSequence, int maxBounces)
{
    return PathIntegrator_Li(ray, rndSequence, _MaxBounces);
}

float3 ACESToneMapping(float3 color, float adapted_lum)
{
    const float A = 2.51f;
    const float B = 0.03f;
    const float C = 2.43f;
    const float D = 0.59f;
    const float E = 0.14f;

    color *= adapted_lum;
    return (color * (A * color + B)) / (color * (C * color + D) + E);
}


inline void AddSample(float2 pixel,Spectrum radiance,float currentWeight)
{
    radiance = _Accumulation[pixel].xyz * (1.0f - currentWeight) + radiance * currentWeight;
    
    _Accumulation[pixel] = float4(radiance, 1.0f);
    
    _RenderTarget[pixel] = float4(radiance, 1.0f);
}


[shader("raygeneration")]
void VisibleRayGen()
{
    float2 pixel = DispatchRaysIndex().xy * float2(1, -1) + float2(0, DispatchRaysDimensions().y);
    const uint LinearIndex = DispatchRaysIndex().y * (int) DispatchRaysDimensions().x + DispatchRaysIndex().x;
    
    RandomSequence rndSequence;
    
    // random sobol init
    RandomSequence_Initialize(rndSequence, LinearIndex, _TemporalSeed);
    
    Spectrum radiance = RayIntegrator_Li(GeneratePrimaryRay(rndSequence), rndSequence, _MaxBounces);
    
    float currentWeight = (_AccumulatedFrames > 1) ? 1.0f / (float) _AccumulatedFrames : 1.0f;
    
    // 渲染指定帧后停止，达到查看spp效果
    currentWeight *= (_AccumulatedFrames >= g_max_accumulated_frames) ? 0 : 1;
    AddSample(pixel, radiance,currentWeight);
    return;
}


[shader("miss")]
void MissShader(inout PathVertexPayload payload : SV_RayPayload)
{
    payload.SetMiss();
}
#endif