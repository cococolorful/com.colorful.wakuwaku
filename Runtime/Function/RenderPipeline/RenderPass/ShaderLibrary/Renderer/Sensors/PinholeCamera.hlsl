#ifndef PINHOLE_CAMERA_HLSL
#define PINHOLE_CAMERA_HLSL

#include"../SceneData.hlsl"
#include"../../../ShaderLibrary/Ray.hlsl"
RayDesc GenPinholeCameraRay(float2 uvInNDC)
{
        Ray as = { _CamPosW, 0.f, float3(1,0,0),1e+9 };
        
    RayDesc ray;
    ray.Origin = _CamPosW;
    ray.TMin = 0.f;
    ray.TMax = 1e+9;

        as.dir = normalize(mul(_InvCameraViewProj, float4(uvInNDC, 0, 1))).xyz;
        return as.ToRayDesc();
   // return ray;
}

#endif