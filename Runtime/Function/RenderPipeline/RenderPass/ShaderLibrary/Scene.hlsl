#ifndef SCENE_HLSL
#define SCENE_HLSL


////////////////////
/// Data Input
////////////////////
//cbuffer CameraData
//{
//    float4x4 _InvCameraViewProj;
//    float3 _CamPosW;
//    float _CamPad0;
//};
cbuffer CameraData
{
    float4x4 g_camera_view_matrix;
    float4x4 g_camera_prev_view_matrix;
    float4x4 g_camera_proj_matrix;
    float4x4 g_camera_view_proj_matrix;
    float4x4 g_camera_inv_view_proj_matrix;
    float4x4 g_camera_view_proj_no_jitter_matrix;
    float4x4 g_camera_prev_view_proj_no_jitter_matrix;
    float4x4 g_proj_no_jitter_matrix;

    float3 g_camera_pos_world;
     
}

struct Camera
{
    float3 GetPosition()
    {
        return g_camera_pos_world;
    }

    float4x4 GetViewProj()
    {
        return g_camera_view_proj_matrix;
    }
};

//RaytracingAccelerationStructure g_scene_bvh;

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