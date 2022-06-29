#ifndef INPUT_HLSL
#define INPUT_HLSL

cbuffer UnityPerDraw
{
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
}
cbuffer UnityPerCamera
{
    float4x4 glstate_matrix_projection_inv;
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    
    float4x4 _PV_Inv_NoJitter;
    float4x4 _Last_PV_NoJitter;
    
    float3 _CameraPosW;
}

inline float4x4 GetCameraView()
{
    return unity_MatrixV;
}

inline float4x4 GetCameraProj()
{
    return glstate_matrix_projection;
}
inline float4x4 GetCameraProjInv()
{
    return glstate_matrix_projection_inv;
}
inline float4x4 GetCameraProjView()
{
    return mul(GetCameraProj(), GetCameraView());
}
inline float4x4 GetObjectToWorld()
{
    return unity_ObjectToWorld;
}
inline float4x4 GetWorldToObject()
{
    return unity_WorldToObject;
}
float4x4 GetPrevProjViewMat()
{
    return _Last_PV_NoJitter;
}

//////////////////////
/// SamplerState Define
//////////////////////
SamplerState my_linear_clamp_sampler;
SamplerState sampler_linear_repeat;

inline SamplerState GetLinearClampSampler()
{
    return sampler_linear_repeat;
}

float3 NormalSampleToWorldSpace(float3 normalMapSample,
    float3 unitNormalW,
    float4 tangentW)
{
    float3 normalT = normalize(2.0f * normalMapSample - 1.0f);
    return unitNormalW;
    //return tangentW*10;
    //return normalT;
    float3 N = unitNormalW;
    float3 T = normalize(tangentW.xyz - dot(tangentW.xyz, N) * N);
    float3 B = cross(N, T);
    
    float3x3 TBN = float3x3(T, B, N);
    
    float3 bumpedNormalW = mul(normalT,TBN);
    return normalize(bumpedNormalW);
              
}

#endif


