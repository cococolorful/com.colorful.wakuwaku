#ifndef SCENE_HLSL
#define SCENE_HLSL

#include"../ShaderLibrary/Camera.hlsl"

#ifdef RAYTRACING
    RaytracingAccelerationStructure g_scene_bvh;
#endif 

StructuredBuffer<float4x4> g_scene_world_matrices;
StructuredBuffer<float4x4> g_scene_inverse_transpose_world_matrices;
StructuredBuffer<int> g_scene_material_ids;

struct Scene
{
    float4x4 GetWorldMatrix(uint instance_id)
    {
        return g_scene_world_matrices[instance_id];
    }
    float4x4 GetInverseTransposeWorldMatrix(uint instance_id)
    {
        return g_scene_inverse_transpose_world_matrices[instance_id];
    }
    int GetMaterialID(uint instance_id)
    {
        return g_scene_material_ids[instance_id];
    }
    Camera camera;
};



//StructuredBuffer<float4x4> g_scene_world_matrices;
//StructuredBuffer<float4x4> g_scene_world_matrices;
//StructuredBuffer<float4x4> g_scene_world_matrices;
//RWTexture2D<float4> _Accumulation : register(u1);
//RWTexture2D<float4> _RenderTarget;
//cbuffer RayTracingConfig
//{
//    int _TemporalSeed;
//    int _MaxBounces;
//    int _AccumulatedFrames;
//    int _MISMode;
//    bool _EnableAntiAliasing;
//    
//    int _CameraTypeMask; // 0 for PinholeCamera
//}
//
//int _NumTotalInfiniteLight;
//int g_max_accumulated_frames;
//
//Texture2D<float3> g_sky_box;
//StructuredBuffer<float> g_sky_box_pdf;
//
//StructuredBuffer<float> g_sky_box_sampling_prob;
//StructuredBuffer<int> g_sky_box_sampling_alias;
#endif