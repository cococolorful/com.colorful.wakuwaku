#ifndef GBUFFER_PASS_HLSL
#define GBUFFER_PASS_HLSL

#include"Input.hlsl"
#include"DefaultVS.hlsl"

struct GBuffer
{
    float4 wsPos : SV_Target0;
    float4 wsNorm : SV_Target1;
    float4 matDif : SV_Target2;
    float4 matSpec : SV_Target3;
    float4 matEmissive : SV_Target4;
    float4 linearZAndNormal : SV_Target5;
    float4 motionVecFwidth : SV_Target6;
};

    // See Material.h for channel layout
Texture2D _BaseColorTex;
Texture2D _MetallicRoughnessTexture;
Texture2D _EmissiveTex;
Texture2D _NormalMap;

Texture2D _AlbedoTex;
float3 _AlbedoFactor;

// Metallic-Roughness
cbuffer UnityPerMaterial
{
    float4 _BaseColorFactor;
    float4 _MetallicRoughnessFactor; // R - Occlusion  G - Metallic  B - Roughness A - Reserved
    float3 _EmissiveFactor;
    float _pad0;
};

/** This struct describes the geometric data for a specific hit point used for lighting calculations 
*/
struct ShadingData
{
    float3 posW; ///< Shading hit position in world space
    float3 V; ///< Direction to the eye at shading hit
    float3 N; ///< Shading normal at shading hit
    float2 uv; ///< Texture mapping coordinates
    float NdotV; // Unclamped, can be negative

    // Pre-loaded texture data
    float3 diffuse;
    float opacity;
    float3 specular;
    float linearRoughness; // This is the original roughness, before re-mapping. It is required for the Disney diffuse term
    float roughness; // This is the re-mapped roughness value, which should be used for GGX computations
    float3 emissive;
    float4 occlusion;
    float3 lightMap;
    float2 height;
    float IoR;
    bool doubleSidedMaterial;
};
ShadingData initShadingData()
{
    ShadingData sd;
    sd.posW = 0;
    sd.V = 0;
    sd.N = 0;
    sd.uv = 0;
    sd.NdotV = 0;
    
    sd.diffuse = 0;
    //sd.opacity = 1;
    sd.specular = 0;
    sd.linearRoughness = 0;
    sd.roughness = 0;
    sd.emissive = 0;
    sd.occlusion = 1;
    sd.lightMap = 0;
    sd.height = 0;
    sd.IoR = 0;
    sd.doubleSidedMaterial = false;

    return sd;
}
ShadingData PrepareShadingData(VertexOut v,float3 camPosW)
{
    ShadingData sd = initShadingData();
    // Sample the diffuse texture and apply the alpha test
    float4 baseColor = _BaseColorFactor * _BaseColorTex.Sample(GetLinearClampSampler(), v.texCoord);
    ////sd.opacity = m.baseColor.a;
    //
    sd.posW = v.PosW;
    sd.uv = v.texCoord;
    sd.V = normalize(camPosW - v.PosW);
    sd.N = normalize(v.NormalW);
    sd.N = NormalSampleToWorldSpace(_NormalMap.Sample(GetLinearClampSampler(), v.texCoord).xyz, sd.N, v.TangentW);
    
    // Sample the spec texture
     // R - Occlusion; G - Roughness; B - Metalness
    float4 spec = _MetallicRoughnessFactor * _MetallicRoughnessTexture.Sample(GetLinearClampSampler(), v.texCoord);
    
#ifdef DIFFUSE
    sd.diffuse = _AlbedoFactor * _AlbedoTex.Sample(GetLinearClampSampler(), v.texCoord);
#else
    sd.diffuse = lerp(baseColor.rgb, float3(0.0f, 0.0f, 0.0f), spec.b);
#endif    
    // UE4 uses 0.08 multiplied by a default specular value of 0.5 as a base, hence the 0.04
    sd.specular = lerp(float3(0.04f, 0.04f, 0.04f), baseColor.rgb, spec.b);
    sd.linearRoughness = spec.g;
    
    sd.NdotV = dot(sd.N, sd.V);

    return sd;
}
GBuffer GBufferPS(VertexOut vsOut)
{
    ShadingData hitPt = PrepareShadingData(vsOut, _CameraPosW);
    // Dump out our G buffer channels
    GBuffer gBufOut;
    gBufOut.wsPos = float4(hitPt.posW, 1.f);
    gBufOut.wsNorm = float4(hitPt.N, length(hitPt.posW - _CameraPosW));
    gBufOut.matDif = float4(hitPt.diffuse, 1);
    gBufOut.matSpec = float4(hitPt.specular, hitPt.linearRoughness);
    gBufOut.matEmissive = float4(hitPt.emissive, 0.f);
    //gBufOut.matDif = _BaseColorFactor;
    int2 ipos = int2(vsOut.PosH.xy);
    const float2 pixelPos = ipos + float2(0.5, 0.5);
    const float4 prevPosH = vsOut.prevPosH;
    return gBufOut;
}
#endif