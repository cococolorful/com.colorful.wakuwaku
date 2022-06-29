#ifndef Lit_CLOSTEST_HLSL
#define Lit_CLOSTEST_HLSL
//
//#include "RTCommon.hlsl"
//#include "Input.hlsl"
//#include "SampleTransformUtil.hlsl"
//#include "UnityRaytracingMeshUtils.cginc"
//
//struct AttributeData
//{
//    float2 barycentrics;
//};
//
//struct IntersectionVertex
//{
//                    // Object space normal of the vertex
//    float3 normalOS;
//    float2 texCoord;
//};
//                // See Material.h for channel layout
//Texture2D _BaseColorTex;
//Texture2D _MetallicRoughnessTexture;
//Texture2D _EmissiveTex;
//Texture2D _NormalMap;
//
//                // Metallic-Roughness
//cbuffer UnityPerMaterial
//{
//    float4 _BaseColorFactor;
//    float4 _MetallicRoughnessFactor; // R - Occlusion  G - Metallic  B - Roughness A - Reserved
//    float3 _EmissiveFactor;
//    float _pad0;
//};
//
//void FetchIntersectionVertex(uint vertexIndex, out IntersectionVertex outVertex)
//{
//    outVertex.normalOS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
//    outVertex.texCoord = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
//}
//
//#define INTERPOLATE_RAYTRACING_ATTRIBUTE(v0,v1,v2,b) v0 * b.x + v1 * b.y + v2 * b.z 
//                
//
//[shader("closesthit")]
//void ClosestHitShader(inout MaterialClosestHitPayload rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
//{
//    rayIntersection.HitT = RayTCurrent();
//
//    // Fetch the indices of the currentr triangle
//    uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
//
//    // Fetch the 3 vertices
//    IntersectionVertex v0, v1, v2;
//    FetchIntersectionVertex(triangleIndices.x, v0);
//    FetchIntersectionVertex(triangleIndices.y, v1);
//    FetchIntersectionVertex(triangleIndices.z, v2);
//
//    // Compute the full barycentric coordinates
//    float3 barycentricCoordinates = float3(1.0 - attributeData.barycentrics.x - attributeData.barycentrics.y, attributeData.barycentrics.x, attributeData.barycentrics.y);
//
//    float3 normalOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normalOS, v1.normalOS, v2.normalOS, barycentricCoordinates);
//    float3x3 objectToWorld = (float3x3) ObjectToWorld3x4();
//    float3 normalWS = normalize(mul(objectToWorld, normalOS));
//
//    // back face
//    if (dot(normalWS, WorldRayDirection())<0)
//    {
//#ifndef EnableDoubleSide
//        MaterialClosestHitPayload hitInfo = (MaterialClosestHitPayload) 0;
//    
//        RayDesc ray;
//        ray.Origin = WorldRayOrigin();
//        ray.Direction = WorldRayDirection();
//        ray.TMin = RayTCurrent() + 0.001;
//        ray.TMax = 1e16f;
//        
//        TraceRay(_SceneBVH, RAY_FLAG_NONE, 0xFF, 0, 1, 0, ray, hitInfo);
//        rayIntersection = hitInfo;
//        return;
//#endif
//    }
//    else
//    {
//        float2 texCoord = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord, v1.texCoord, v2.texCoord, barycentricCoordinates);
//    
//        float4 BaseColor = _BaseColorFactor * _BaseColorTex.SampleLevel(GetLinearClampSampler(), texCoord, 0);
//    // R - Occlusion; G - Roughness; B - Metalness
//        float4 spec = _MetallicRoughnessFactor * _MetallicRoughnessTexture.SampleLevel(GetLinearClampSampler(), texCoord, 0);
//                    
//        rayIntersection.WorldPosition = WorldRayOrigin() + RayTCurrent() * WorldRayDirection();
//        rayIntersection.Metallic = spec.b;
//        rayIntersection.BaseColor = BaseColor.xyz;
//        rayIntersection.Roughness = spec.g;
//        rayIntersection.WorldNormal = normalWS;
//        rayIntersection.Radiance = _EmissiveFactor * _EmissiveTex.SampleLevel(GetLinearClampSampler(), texCoord, 0).xyz;
//    }
//    
//}
//
#endif