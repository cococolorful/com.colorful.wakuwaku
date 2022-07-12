#ifndef PATH_TRACER_HLSL
#define PATH_TRACER_HLSL

#include "../RTCommon.hlsl"
#include "../SceneUtil.hlsl"
//#include "../Materials/MaterialSampling.hlsl"
#include "../Lights/LightSampling.hlsl"
#include "../Util/Spectrum.hlsl"



Spectrum  PathIntegrator_Li(in RayDesc ray, inout RandomSequence rndSequence, int maxBounces)
{
    Spectrum throughput = 1.0f;
    Spectrum radiance = 0;
    
    // If the last vertex of the previous path is specular,the direct illumination estimate of the previous path is basiclly 0
    // So the last direct illumination estimate is simply emitted(good for gpu？the thread of same warp,diverence).
    // If the light source is hit this time(or enviroment),just to make up for the direct illumination estimate of previous path
    bool specularBounce = false;
    PathVertexPayload payload;
    payload.BRDFSamplingResult.pdf = 1; // 第一次击中面光源会进行特别处理
    int bounce = 0;
    while(true)
    {
        payload = IntersectPathVertex(payload.BRDFSamplingResult.pdf, ray, rndSequence);
        
        // BRDF importance sampling estimation
        {
            if (payload.IsMiss())
            {
                float3 Le = EnvironmentLe(ray.Direction);
                if (bounce == 0)
                    radiance += throughput * Le;
                else
                {
                    //Le = float3(0, 0, 0);
                    float enviromental_light_pdf = EnvironmentPDF(ray.Direction);
                    float area_light_pdf = 0; // area light sampling alway be 0 for environment tex
                    float mis_weight =   PowerHeuristic(1, payload.BRDFSamplingResult.pdf, 1, enviromental_light_pdf, 1, area_light_pdf);
                    // Compute MIS wight for environment tex
                    //if (any(isnan(throughput)))
                    //    return float3(0, 0, 1000);
                    //if (any(isnan(Le)) || any(isinf(Le)))
                    //    return float3(1000, 0, 0);
                    radiance += throughput * mis_weight * Le;
                    //if (any(isnan(radiance)))
                    //    return float3(0,1000,  0);

                }
            // Terminate path cause ray escaped
                break;
            }
            
            if (payload.HitAreaLight())
            {
                if (bounce == 0)
                    radiance += throughput * payload.Le();
                else
                {
                // compute MIS weight for area light
                    float area_light_pdf = (1.0 / GetLightCount()) * payload.area_light_pdf_in_solid_;
                    float enviromental_light_pdf = 0; // environment tex sampling alway be 0 for area light
                    float mis_weight = PowerHeuristic(1, payload.BRDFSamplingResult.pdf, 1, area_light_pdf, 1, enviromental_light_pdf);
            
                    radiance += throughput * mis_weight * payload.Le();
                }
            }
        }
        
        
        // Terminate path cause maxDepth was reached
        if (bounce++ == maxBounces)
            break;
        radiance += payload.DirectLightEstimation * throughput;
       // radiance = payload.Normal;
       // break;
        if (payload.BRDFSamplingResult.pdf <= 0 )
        {
            if (!(payload.BRDFSamplingResult.pdf > 0))
            {
			// No valid direction -- we are doneelse
                //radiance = float3(1, 0, 0);
                break;
            }
            if ( asuint(payload.BRDFSamplingResult.pdf) > 0x7F800000)
            {
			// Pdf became invalid (either negative or NaN)
                radiance = float3(0, 0, 11100);
                break;
            }
            if (isnan(payload.BRDFSamplingResult.pdf))
            {
			// Pdf became invalid (either negative or NaN)
               radiance = float3(0, 0, 11100);
                break;
            }
            if (payload.BRDFSamplingResult.pdf < 0 || asuint(payload.BRDFSamplingResult.pdf) > 0x7F800000)
            {
			// Pdf became invalid (either negative or NaN)
               radiance = float3(0, 1100, 0);
                break;
            }
            
            break;
        }
        
        ray = UpdateRay(payload.Position, payload.Normal, payload.BRDFSamplingResult.wi);
        //if (any(isnan(payload.BRDFSamplingResult.pdf)))
        //    return float3(0, 1000, 0);
        // TODO: reduce the memory of the payload by computing 
        throughput *= payload.BRDFSamplingResult.f * SafeDot(payload.BRDFSamplingResult.wi, payload.Normal) / payload.BRDFSamplingResult.pdf;
        //if (any(isnan(throughput)))
        //    return float3(0, 0, 1000);
        //return radiance;
        if (bounce > 5)
        {
            float rr_p = min(0.95f, throughput[0]);
            if (rr_p < RandomSequence_GenerateSample1D(rndSequence))
                break;
            else
                throughput*= (1.0f / rr_p);
        }
    }
    //if (any(radiance))
    //    radiance = float3(110, 0, 0);
    //if (any(isnan(radiance)))
   
    //    radiance = float3(0, 0, 110);
    if (any(isnan(radiance)))
        return float3(1000,0,0);
    //if (any(radiance < 0))
    //    return float3(0, 10, 0);
    radiance = max(radiance, float3(0, 0, 0));
    
    radiance = min(radiance, float3(10, 10, 10));
    return radiance;
}

#endif