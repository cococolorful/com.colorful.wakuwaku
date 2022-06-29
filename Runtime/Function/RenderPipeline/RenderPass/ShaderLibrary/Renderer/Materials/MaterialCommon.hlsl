#ifndef MATERIAL_COMMON_HLSL
#define MATERIAL_COMMON_HLSL
#define  FORCE_DIFFUSE

#include "../RTCommon.hlsl"

#include "../SceneUtil.hlsl"
#include "../Unity/UnityRayTracingMeshUtils.cginc"
#include "../Util/Spectrum.hlsl"
#include "../SampleTransformUtil.hlsl"
float3 CosineSampleHemisphere(inout RandomSequence rndSequence)
{
    float2 uv = RandomSequence_GenerateSample2D(rndSequence);
    
    float3 dir;
    dir.x = sqrt(uv.x) * cos(2 * M_PI * uv.y);
    dir.y = sqrt(uv.x) * sin(2 * M_PI * uv.y);
    dir.z = sqrt(1 - uv.y);
    return dir;
}
// [ Duff et al. 2017, "Building an Orthonormal Basis, Revisited" ]
// Discontinuity at TangentZ.z == 0
float3x3 GetTangentBasis(float3 TangentZ)
{
    const float Sign = TangentZ.z >= 0 ? 1 : -1;
    const float a = -rcp(Sign + TangentZ.z);
    const float b = TangentZ.x * TangentZ.y * a;
	
    float3 TangentX = { 1 + Sign * a * Pow2(TangentZ.x), Sign * b, -Sign * TangentZ.x };
    float3 TangentY = { b, Sign + a * Pow2(TangentZ.y), -TangentZ.y };

    return float3x3(TangentX, TangentY, TangentZ);
}

// [Frisvad 2012, "Building an Orthonormal Basis from a 3D Unit Vector Without Normalization"]
// Discontinuity at TangentZ.z < -0.9999999f
float3x3 GetTangentBasisFrisvad(float3 TangentZ)
{
    float3 TangentX;
    float3 TangentY;

    if (TangentZ.z < -0.9999999f)
    {
        TangentX = float3(0, -1, 0);
        TangentY = float3(-1, 0, 0);
    }
    else
    {
        float A = 1.0f / (1.0f + TangentZ.z);
        float B = -TangentZ.x * TangentZ.y * A;
        TangentX = float3(1.0f - TangentZ.x * TangentZ.x * A, B, -TangentZ.x);
        TangentY = float3(B, 1.0f - TangentZ.y * TangentZ.y * A, -TangentZ.y);
    }

    return float3x3(TangentX, TangentY, TangentZ);
}

float3 TangentToWorld(float3 Vec, float3 TangentZ)
{
    return mul(Vec, GetTangentBasis(TangentZ));
}



struct MaterialClosestHitPayload
{
    //Negative for miss
    float HitT;
    bool IsMiss()
    {
        return HitT < 0;
    }
    bool IsHit()
    {
        return !IsMiss();
    }
    void SetMiss()
    {
        HitT = -1;
    }
    
    float3 Radiance;
    float3 WorldNormal;
    float3 WorldPosition;
    
    float3 BaseColor;
    
    float3 DiffuseColor; // Derived
    float3 SpecularColor; // Derived

    float Metallic;
    float Roughness;
    
    int ShadingModelID;
    
    float GetMetallic()
    {
        return Metallic;
    }
    float3 GetRadiance()
    {
        return Radiance;
    }
    float GetRoughness()
    {
        return Roughness;
    }
    float3 GetDiffuseColor()
    {
        return DiffuseColor;
    }
    float3 GetWorldPosition()
    {
        return WorldPosition;
    }
    float3 GetBaseColor()
    {
        return BaseColor;
    }
    float3 GetWorldNormal()
    {
        return WorldNormal;
    }
    bool IsLambert()
    {
        return ShadingModelID == 0;
    }
    bool IsDielectric()
    {
        return ShadingModelID = 1;
    }
    bool IsSpecular()
    {
        return Roughness < 0.01;
    }

    
    // TODO:
    
    // The radiance emitted by the mesh light source in the direction _w_.
    //SampledSpectrum Le(float3 w,in SampledWaveLengths lambda)
    //{
    //    
    //}
};


static const int ShadingModel_Diffuse = 0;
static const int ShadingModel_MetalRoughness = 0;

#endif