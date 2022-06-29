#ifndef DEFAULTVS_HLSL
#define DEFAULTVS_HLSL

#include"Input.hlsl"
struct VertexIn
{
    float3 PosL : POSITION;
    float3 NormalL : NORMAL;
    float4 TangentL : TANGENT;
    float2 texCoord : TEXCOORD;
};
struct VertexOut
{
    float4 PosH : SV_POSITION;
    float3 PosW : NORMAL0;
    float3 NormalW : NORMAL1;
    float4 TangentW : TANGENT;
    float2 texCoord : TEXCOORD;
    float4 prevPosH : NORMAL2;
};

float4x4 GetWorldMat(VertexIn vIn)
{
    float4x4 worldMat = GetObjectToWorld();
    return worldMat;
}
float4x4 GetWorldInvTransposeMat(VertexIn vIn)
{
    return transpose(GetWorldToObject());
}
float4x4 _localToWorldPrev;

float4x4 GetPrevWorldMat(VertexIn vIn)
{
    return _localToWorldPrev;
}

VertexOut DefaultVS(VertexIn vIn)
{
    VertexOut o;
    float4x4 worldMat = GetWorldMat(vIn);
    float4 posW = mul(worldMat, float4(vIn.PosL, 1.0f));
    o.PosW = posW.xyz;
    o.PosH = mul(GetCameraProjView(), posW);
    
    o.texCoord = vIn.texCoord;
    o.NormalW = normalize(mul(GetWorldInvTransposeMat(vIn), float4(vIn.NormalL, 0))).xyz;
    o.TangentW = normalize(mul(GetWorldMat(vIn), vIn.TangentL));
    float4 prePos = float4(vIn.PosL, 1.0f);
    float4 prePosW = mul(GetPrevWorldMat(vIn), prePos);
    o.prevPosH = mul(GetPrevProjViewMat(), prePosW);
    return o;
}


#endif