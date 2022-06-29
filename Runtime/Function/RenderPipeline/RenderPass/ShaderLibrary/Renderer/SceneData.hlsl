#ifndef SCENE_DATA_HLSL
#define SCENE_DATA_HLSL


////////////////////
/// Data Input
////////////////////
cbuffer CameraData
{
    float4x4 _InvCameraViewProj;
    float3 _CamPosW;
    float _CamPad0;
};
RaytracingAccelerationStructure _SceneBVH;

RWTexture2D<float4> _Accumulation : register(u1);
RWTexture2D<float4> _RenderTarget;
cbuffer RayTracingConfig
{
    int _TemporalSeed;
    int _MaxBounces;
    int _AccumulatedFrames;
    int _MISMode;
    bool _EnableAntiAliasing;
    
    int _CameraTypeMask; // 0 for PinholeCamera
}

int _NumTotalInfiniteLight;
int g_max_accumulated_frames;

Texture2D<float3> g_sky_box;
StructuredBuffer<float> g_sky_box_pdf;

StructuredBuffer<float> g_sky_box_sampling_prob;
StructuredBuffer<int> g_sky_box_sampling_alias;
#endif