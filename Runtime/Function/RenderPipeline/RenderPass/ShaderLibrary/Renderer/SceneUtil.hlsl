#ifndef SCENE_UTIL_HLSL
#define SCENE_UTIL_HLSL

#include "Unity/UnityRaytracingMeshUtils.cginc"


struct AttributeData
{
    float2 barycentrics;
};

/// Given a vector n, outputs a coordinate basis that consists of three orthonormal unit vector
/// The approach here is based on Frisvad's paper
/// "Building an Orthonormal Basis from a 3D Unit Vector Without Normalization"
/// https://backend.orbit.dtu.dk/ws/portalfiles/portal/126824972/onb_frisvad_jgt2012_v2.pdf
float3x3 Frisvad(in float3 n)
{
    float3 b1;
    float3 b2;
    if (n[2] < (-1.0 + 1e-6))
    {
        b1 = float3(0, -1, 0);
        b2 = float3(-1, 0, 0);
    }
    else
    {
        const float a = 1.0 / (1.0 + n[2]);
        const float b = -n[0] * n[1] * a;
        b1 = float3(1.0 - n[0] * n[0] * a, b, -n[0]);
        b2 = float3(b, 1.0 - n[1] * n[1] * a, -n[1]);
    }
    return float3x3(b1, b2, n);
}
// a "Frame" is a coordinate basis that consists of three orthonormal unit vector
// It is is useful for sampling points on a hemisphere
struct Frame
{
    float3x3 normalSpace;
    
    void Init(float3 n)
    {
        normalSpace = Frisvad(n);
    }
    /// Project a vector to a frame's local coordinates.
    float3 ToLocal(float3 v)
    {
        return mul(normalSpace, v);
    }
    /// Convert a vector in a frame's local coordinates to the reference coordinate the frame is in.
    float3 ToWorld(float3 v)
    {
        return mul(v, normalSpace);
    }
    
    void Filp()
    {
        normalSpace = normalSpace * -1;

    }
    
    float3 Normal()
    {
        return ToWorld(float3(0, 0, 1));
    }
};

struct NormalTexCoord
{
    // Object space normal of the vertex
    Frame shadingFrame;
    float3 position;
    float2 texCoord;
    float area;
};
#define INTERPOLATE_RAYTRACING_ATTRIBUTE(v,b) (v[0] * b.x + v[1] * b.y + v[2] * b.z )
//void FetchIntersectionVertex(uint vertexIndex,out float3 position, out float3 normal, out float2 texCoord)
//{
//    position = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
//    normal = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
//    texCoord = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
//}
//void FetchIntersectionVertex(uint vertexIndex[], out float3 position[], out float3 normal[], out float2 texCoord[])
//{
//    
//}
NormalTexCoord GetShadingPoint(in AttributeData attributeData)
{
    // Fetch the indices of the currentr triangle
    uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
    uint triangle_indices[3];
    triangle_indices[0] = triangleIndices.x;
    triangle_indices[1] = triangleIndices.y;
    triangle_indices[2] = triangleIndices.z;
    
    // Fetch the 3 vertices
    float3 n[3];
    
    float2 uv[3];
    float3 pos[3];
    {
        [unroll]
        for (int i = 0; i < 3; i++)
        {
            pos[i] = UnityRayTracingFetchVertexAttribute3(triangle_indices[i], kVertexAttributePosition);
            n[i] = UnityRayTracingFetchVertexAttribute3(triangle_indices[i], kVertexAttributeNormal);
            uv[i] = UnityRayTracingFetchVertexAttribute2(triangle_indices[i], kVertexAttributeTexCoord0);
        }
        
    }
        // Compute the full barycentric coordinates
    float3 barycentricCoordinates = float3(1.0 - attributeData.barycentrics.x - attributeData.barycentrics.y, attributeData.barycentrics.x, attributeData.barycentrics.y);
           
    
    float3 normalOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(n, barycentricCoordinates);
    float3x3 objectToWorld = (float3x3) ObjectToWorld3x4();
            
    NormalTexCoord shadingPoint;
    shadingPoint.shadingFrame.Init(normalize(mul(objectToWorld, normalOS)));
    shadingPoint.texCoord = INTERPOLATE_RAYTRACING_ATTRIBUTE(uv, barycentricCoordinates);
    
    [unroll]
    for (int i = 0; i < 3;i++)
        pos[i] = mul(ObjectToWorld3x4(), float4(pos[i], 1.0f)).xyz;
    
    shadingPoint.position = WorldRayOrigin() + RayTCurrent() * WorldRayDirection();
    shadingPoint.position = INTERPOLATE_RAYTRACING_ATTRIBUTE(pos, barycentricCoordinates);
    
    shadingPoint.area = 1.0f / (0.5f * length(cross(pos[1] - pos[0], pos[2] - pos[0])));
    return shadingPoint;
}

#include "RTCommon.hlsl"
#include "SceneData.hlsl"
#include "Util/Spectrum.hlsl"
RayDesc UpdateRay(float3 hitPosition, float3 hitNormal, float3 outDir)
{
    RayDesc ray;
    ray.Origin = hitPosition + hitNormal * 0.0000001f;
    ray.Direction = outDir;
    ray.TMin = 0;
    ray.TMax = 1e16f;
    return ray;

}
RayDesc UpdateRay(float3 hitPositionFixed, float3 outDir)
{
    RayDesc ray;
    ray.Origin = hitPositionFixed;
    ray.Direction = outDir;
    ray.TMin = 0.001f;
    ray.TMax = 1e16f;
    return ray;

}

struct BSDFSample
{
    Spectrum f;
    float3 wi;
    float pdf;
};
struct BSDFEval
{
    float3 f;
    float pdf;
};
struct PathVertexPayload
{
    BSDFSample BRDFSamplingResult;
    float3 Position;
    Spectrum DirectLightEstimation;
    float3 Normal; // TODO:或许可以省掉，因为normal主要用于更新光线（自遮挡）和brdf important sampling 计算中的cosine项，brdf中就可以直接将BRDFSampling中的f更改为f*cosine
    RandomSequence RndSequence;
    
    int closestId;
    
    float t;
    
    void SetShadingPoint(NormalTexCoord shadingPoint)
    {
        Position = shadingPoint.position;
        Normal = shadingPoint.shadingFrame.Normal();
    }
    
    float area_light_pdf_in_solid_;
    
    //*
    Spectrum radiance_; 
    bool HitAreaLight()
    {
        return any(radiance_ > 0);
    }
    
    //*
    bool hit_environment_ ;
    void SetMiss()
    {
        hit_environment_ = true;
    }
    bool IsMiss()
    {
        return hit_environment_ == true;
    }
    
    
    void SetNormalTest()
    {
        closestId = 0;
    }
    void SetVisibility()
    {
        closestId = 1;
    }
    bool IsNormalTest()
    {
        return closestId == 0;
    }
    bool IsVisibility()
    {
        return closestId == 1;
    }
    Spectrum Le()
    {
        return radiance_;
    }
};

PathVertexPayload IntersectPathVertex(float pdf_brdf_in_solid_angle, RayDesc ray, inout RandomSequence rnd)
{
    
    PathVertexPayload hitInfo = (PathVertexPayload) 0;
    hitInfo.RndSequence = rnd;
    hitInfo.SetNormalTest();
    hitInfo.BRDFSamplingResult.pdf = pdf_brdf_in_solid_angle;
    hitInfo.BRDFSamplingResult.wi = ray.Direction;
    hitInfo.hit_environment_ = false;
    TraceRay(_SceneBVH, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, ray, hitInfo);
    rnd = hitInfo.RndSequence;
    return hitInfo;
}

// 
float TraceVisibilityRay(float3 hitPosition,float3 hitNormal,float3 outDir,float3 TMax)
{
    RayDesc directRay = UpdateRay(hitPosition, hitNormal, outDir);
    directRay.TMax = TMax;
    
    PathVertexPayload hitInfo = (PathVertexPayload) 0;
    hitInfo.SetVisibility(); //RAY_FLAG_NONE
    hitInfo.t = TMax;
    
    TraceRay(_SceneBVH, RAY_FLAG_NONE, 0xFF, 0, 1, 0, directRay, hitInfo);
    
    if (hitInfo.IsMiss())
        return 1.0f;

    return  0.0;

}
#endif