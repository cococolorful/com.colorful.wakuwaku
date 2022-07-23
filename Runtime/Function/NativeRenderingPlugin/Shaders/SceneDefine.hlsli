#ifndef SCENEDEFINE_HLSLI
#define SCENEDEFINE_HLSLI

struct Camera
{
    float4x4 view_matrix;
    float4x4 prev_view_matrix;
    float4x4 proj_matrix;
    float4x4 view_proj_matrix;
    float4x4 inv_view_proj_matrix;
    float4x4 view_proj_no_jitter_matrix;
    float4x4 prev_view_proj_no_jitter_matrix;
    float4x4 jitter_matrix;

    float3 pos_world;
    float near_z;
    
    float3 right;
    float jitter_x;
    
    float3 up;
    float jitter_y;
    
    float3 forward;
    float far_z;
    
    float3 CameraGetPosition()
    {
        return pos_world;
    }

    float4x4 CameraGetViewProj()
    {
        return view_proj_matrix;
    }
    float4x4 CameraGetPervViewProj()
    {
        return prev_view_proj_no_jitter_matrix;
    }
};
ConstantBuffer<Camera> g_camera : register(b0);

StructuredBuffer<float4x4> g_world_matrix : register(t0);
StructuredBuffer<float4x4> g_prev_world_matrix : register(t1);
StructuredBuffer<float4x4> g_world_inv_transpose_matrix : register(t2);

struct Material
{
    int albedo_idx;
};
StructuredBuffer<Material> g_materials : register(t3);

struct InstanceData
{
    int transform_id;
    int material_id;   
    
    float4x4 GetWorldMatrix()
    {
        return g_world_matrix[transform_id];
    }
    
    float4x4 GetPrevWorldMatrix()
    {
        return g_prev_world_matrix[transform_id];
    }
    float4x4 GetWorldInvTransMatrix()
    {
        return g_world_inv_transpose_matrix[transform_id];
    }
};
StructuredBuffer<InstanceData> g_instance_data : register(t4);

Texture2D g_textures[] : register(t5);

#endif