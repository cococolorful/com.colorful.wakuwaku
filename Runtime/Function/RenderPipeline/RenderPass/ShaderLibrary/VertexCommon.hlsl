#ifndef VERTEX_COMMON_HLSL
#define VERTEX_COMMON_HLSL

#include"Input.hlsl"
struct VertexIn
{
    float3 PosL : POSITION;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float2 texCoord : TEXCOORD;
};
struct VertexOut
{
    float3 PosH : SV_POSITION;
    float3 PosW : POSITION;
    float3 NormalW : NORMAL;
    float2 texCoord : TEXCOORD;
};
#endif