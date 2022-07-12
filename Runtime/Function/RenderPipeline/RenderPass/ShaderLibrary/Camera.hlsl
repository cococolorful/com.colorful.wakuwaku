#ifndef CAMERA_HLSL
#define CAMERA_HLSL
#include"../ShaderLibrary/Ray.hlsl"

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
    float g_camera_near_z;
    
    float3 g_camera_right;
    float g_camera_jitter_x;
    
    float3 g_camera_up;
    float g_camera_jitter_y;
    
    float3 g_camera_forward;
    float g_camera_far_z;
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
    float3 ComputeNonNormalizedRayDirPinhole(uint2 pixel, uint2 frameDim, bool applyJitter = true)
    {
        // calculate the value of this pixel in screen space in [0,1]
        float2 p = (pixel + float2(0.5f, 0.5f)) / frameDim;
        
        if (applyJitter)
            p += float2(g_camera_jitter_x, g_camera_jitter_y);
        float2 ndc = float2(2, -2) * p + float2(-1, 1);
        
        return ndc.x * g_camera_right + ndc.y * g_camera_up + g_camera_forward;
        
        //return float3(9, 0, 0);
        //ndc.x =

        
    }
    Ray ComputeRayPinhole(uint2 pixel,uint2 frameDim,bool applyJitter = true)
    {
        Ray ray;
        
        ray.origin = g_camera_pos_world;
        ray.dir = normalize(ComputeNonNormalizedRayDirPinhole(pixel,frameDim,applyJitter));
        
        float inv_cos = 1.f / dot(normalize(g_camera_forward), ray.dir);
        ray.t_min = g_camera_near_z * inv_cos;
        ray.t_max = g_camera_far_z * inv_cos;
        
        return ray;
    }
    
    
};
#endif