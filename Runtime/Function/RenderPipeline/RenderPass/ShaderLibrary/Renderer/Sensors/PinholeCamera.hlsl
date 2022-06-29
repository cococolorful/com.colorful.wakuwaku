#ifndef PINHOLE_CAMERA_HLSL
#define PINHOLE_CAMERA_HLSL

#include"../SceneData.hlsl"
RayDesc GenPinholeCameraRay(float2 uvInNDC)
{
    RayDesc ray;
    ray.Origin = _CamPosW;
    ray.TMin = 0.f;
    ray.TMax = 1e+9;

    ray.Direction = normalize(mul(_InvCameraViewProj, float4(uvInNDC, 0, 1))).xyz;
    return ray;
}

#endif