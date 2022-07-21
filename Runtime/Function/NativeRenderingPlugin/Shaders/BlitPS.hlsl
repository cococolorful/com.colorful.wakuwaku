
Texture2D<float3> src_color : register(t0);
SamplerState BilinearSampler : register(s0);

float4 main(float4 Pos : SV_Position, float2 Tex : TEXCOORD0) : SV_TARGET
{
    return float4(src_color.Sample(BilinearSampler, Tex), 1);
}

