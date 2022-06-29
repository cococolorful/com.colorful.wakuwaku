#ifndef LIGHTS_POINT_LIGHT_HLSL
#define LIGHTS_POINT_LIGHT_HLSL
#include "LightCommon.hlsl"

//struct PointLight
//{
//    SampledSpectrum I;
//    float3 Position;
//    
//    // Point Light Attenuation Without Singularity
//    float _PointLightAttenuation(float distant, float radius)
//    {
//    //return 1.0f / Pow2(distant);
//        return (2.0 / Pow2(radius)) * (1.0 - distant / (sqrt(Pow2(distant) + Pow2(radius))));
//    }
//    void Init(PathTracingLight light, SampledSpectrum s)
//    {
//        I = s;
//        Position = light.Position;
//    }
//    
//    LightLiSample SampleLi(float3 shadingPosition)
//    {
//        LightLiSample ls;
//        ls.pdf = 1.0f;
//        ls.position = Position;
//        
//        { 
//            // TODO:dynamic radius
//            float radius = 0.5f;
//            float d = distance(Position, shadingPosition);
//            
//            float attenuation = _PointLightAttenuation(d, radius);
//            ls.radiance = mul(I,attenuation);        
//        }
//        return ls;
//    }
//    
//    // be called after BRDF Sampling hitted
//    SampledSpectrum Li(float3 shadingPosition)
//    {
//        // TODO:dynamic radius
//        float radius = 0.5f;
//        float d = distance(Position, shadingPosition);
//            
//        float attenuation = _PointLightAttenuation(d, radius);
//
//        return mul(I, attenuation);
//    }
//};

//float3 PointLight_TraceLight(RayDesc Ray, int LightId, inout float HitT)
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