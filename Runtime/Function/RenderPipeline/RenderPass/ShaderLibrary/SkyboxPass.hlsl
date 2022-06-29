#ifndef SKYBOX_HLSL
#define SKYBOX_HLSL
#include"Input.hlsl"
static const float3 gPositions[8] =
{
    float3(-1.0f, -1.0f, -1.0f),
    float3(-1.0f, -1.0f, 1.0f),
    float3(-1.0f, 1.0f, -1.0f),
    float3(-1.0f, 1.0f, 1.0f),
    float3(1.0f, -1.0f, -1.0f),
    float3(1.0f, -1.0f, 1.0f),
    float3(1.0f, 1.0f, -1.0f),
    float3(1.0f, 1.0f, 1.0f),
};
// front to inner
static const uint gIndexMap[36] =
{
    4, 6, 0,
	2, 0, 6,
	
	2, 3, 0,
	1, 0, 3,
	
	1, 5, 0,
	4, 0, 5,
	
	3, 7, 1,
	5, 1, 7,
	
	5, 7, 4,
	6, 4, 7,
	
	6, 7, 2,
	3, 2, 7
};
TextureCube _SkyboxTex;
struct VertexOut
{
    float4 PosH : SV_POSITION;
    float3 TexC : TEXCOORD;
};

VertexOut SkyboxVS(uint VertID : SV_VertexID)
{
    VertexOut vout;

    float3x3 viewWithoutPos = (float3x3) GetCameraView();
    float3 pos = mul(viewWithoutPos, gPositions[gIndexMap[VertID]]);
    vout.PosH = mul(GetCameraProj(), float4(pos, 1.0f));
    vout.PosH.z = 0;
    vout.TexC = gPositions[gIndexMap[VertID]];

    return vout;
}

float4 SkyboxPS(VertexOut pin) : SV_Target
{
    return _SkyboxTex.Sample(GetLinearClampSampler(), normalize(pin.TexC));
}

#endif