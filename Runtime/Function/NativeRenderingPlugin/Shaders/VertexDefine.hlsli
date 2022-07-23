#ifndef VERTEXDEFINE_HLSLI
#define VERTEXDEFINE_HLSLI

struct DefaultVertexIn
{
    float3 PosL : POSITION;
    float3 NormalL : NORMAL;
    float4 TangentL : TANGENT;
    float2 texCoord : TEXCOORD;
};

struct DefaultVertexOut
{
    float4 PosH : SV_POSITION;
    
    float3 PosW : POSITION;
    float3 NormalW : NORMAL;
    float4 TangentW : TANGENT;
    float2 texCoord : TEXCOORD;
    float4 prevPosH : PREVPOSITION;
    
    // Per triangle data
    nointerpolation int instance_id : INSTANCE_ID;
    nointerpolation int material_id : MATERIAL_ID;
    
};
#endif