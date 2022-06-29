#ifndef LIGHTS_DISTANT_LIGHT_HLSL
#define LIGHTS_DISTANT_LIGHT_HLSL
#include "LightCommon.hlsl"

//struct DistantLight
//{
//    Spectrum LEmit;
//    float3 Direction;
//    void Init(PathTracingLight light, SampledSpectrum l)
//    {
//        LEmit = l;
//        Direction = light.Normal;
//    }
//    LightLiSample SampleLi(float3 shadingPositon)
//    {
//        LightLiSample ls;
//        ls.pdf = 1.0f;
//        ls.radiance = LEmit;
//        ls.position = shadingPositon + (-Direction) * 1e8f;
//        
//        return ls;
//    }
//    SampledSpectrum Li(float3 wiRender)
//    {
//        float CosTheta = SafeDot(wiRender, -Direction);
//        if(abs(1-CosTheta) < 1e-3f)
//        {
//            // LEmit.Mul(Scale);
//            return LEmit;
//        }
//        return (SampledSpectrum) 0;
//    }
//};
//float3 DistantLight_TraceLight(RayDesc Ray, int LightId, inout float HitT)
//{
//    if (Ray.TMax == HitT) // we hit the background
//    {
//        float3 LightDirection = GetLightNormal(LightId);
//        
//        float CosTheta = saturate(dot(normalize(Ray.Direction), -LightDirection));
//
//		// Is ray pointing inside the cone of directions?
//        if (abs(1 - CosTheta) < 1e-3f)
//        {
//            return GetLightRadiance(LightId);
//        }
//    }
//    return 0.0;
//}

#endif