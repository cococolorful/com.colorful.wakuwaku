#ifndef LIGHTS_LIGHT_SAMPLING_HLSL
#define LIGHTS_LIGHT_SAMPLING_HLSL
#include "DistantLight.hlsl"
#include "PointLight.hlsl"
#include "../SceneData.hlsl"
struct LightSample
{
    int lightID;
    float pdf;
};
// generate a surface on light source
LightSample SampleLights(inout RandomSequence rndSequence)
{
    LightSample res = (LightSample)0;
    
    // choose a light
    float u = RandomSequence_GenerateSample1D(rndSequence);
    uint lightCount, stride;
    g_light_buffer.GetDimensions(lightCount, stride);
    if (lightCount==0)
        return res;
    
    res.lightID = u * lightCount;
    res.pdf = 1.0f / lightCount;
    return res;
}

int GetLightCount()
{
    uint lightCount, stride;
    g_light_buffer.GetDimensions(lightCount, stride);
    return lightCount;
}
//Spectrum EnvironmentLe(RayDesc ray)
//{
//    float2 uv = SolidAngle2UV(ray.Direction);
//    
//    uint width = 0, height = 0;
//    g_sky_box.GetDimensions(width, height);
//    
//    int2 idx = uv * float2(width, height);
//    int idx_linear = idx.y * width + idx.x;
//    float3 radiance = g_sky_box.SampleLevel(GetLinearClampSampler(), uv, 0).xyz;
//    
//    return radiance;;
//    
//}
Spectrum EnvironmentLe(float3 w)
{
    
    float2 uv = SolidAngle2UV(w);
    
    //uint width = 0, height = 0;
    //g_sky_box.GetDimensions(width, height);
    //
    //int2 idx = uv * float2(width, height);
    //int idx_linear = idx.y * width + idx.x;
    float3 radiance = g_sky_box.SampleLevel(GetLinearClampSampler(), uv, 0).xyz;
    // float vItem = TraceVisibilityRay(vertexPayload.Position, vertexPayload.Normal, env_res.to_light, 1e16f);
    //if (any(isnan(radiance)))
    //    return float3(0,111,0);
    //return float3(0, 0, 0);
    return radiance;
    
    //float light_count = GetLightCount();
    //float pdf_light_sampling = (1.0 / light_count) / g_sky_box_pdf[idx_linear];
    //
    //float wight_brdf_sampling = PowerHeuristic(1, payload.BRDFSamplingResult.pdf, 1, pdf_light_sampling);
    //payload.DirectLightEstimation = radiance;
    //payload.Radiance = radiance;
}
//float EnvironmentPDF(RayDesc ray)
//{
//    float2 uv = SolidAngle2UV(ray.Direction);
//    
//    uint width = 0, height = 0;
//    g_sky_box.GetDimensions(width, height);
//    
//    int2 idx = uv * float2(width, height);
//    int idx_linear = idx.y * width + idx.x;
//    float theta = (1.0 - uv.y) * Pi;
//    float phi = (0.5 - uv.x) * 2 * Pi;
//    float cosTheta = cos(theta);
//    float sinTheta = sin(theta);
//    float sinPhi = sin(phi);
//    float cosPhi = cos(phi);
//
//    return g_sky_box_pdf[idx_linear] / (2 * Pi * Pi * sinTheta);
//    
//   // float wight_brdf_sampling = PowerHeuristic(1, payload.BRDFSamplingResult.pdf, 1, pdf_light_sampling);
//   
//}
//float EnvironmentPDF2(float3 w)
//{
//    
//    float2 uv = SolidAngle2UV(w);
//    
//    uint width = 0, height = 0;
//    g_sky_box.GetDimensions(width, height);
//    
//    int2 idx = uv * float2(width, height);
//    int idx_linear = idx.y * width + idx.x;
//    float theta = (1.0 - uv.y) * Pi;
//    float phi = (0.5 - uv.x) * 2 * Pi;
//    float cosTheta = cos(theta);
//    float sinTheta = sin(theta);
//    float sinPhi = sin(phi);
//    float cosPhi = cos(phi);
//
//    return g_sky_box_pdf[idx_linear] / (2 * Pi * Pi * sinTheta);
//    
//   // float wight_brdf_sampling = PowerHeuristic(1, payload.BRDFSamplingResult.pdf, 1, pdf_light_sampling);
//   
//}
float EnvironmentPDF(float3 w)
{
    float2 uv = SolidAngle2UV(w);
    
    uint width = 0, height = 0;
    g_sky_box.GetDimensions(width, height);
    
    uint x = clamp(uv.x * width, 0, width - 1);
    uint y = clamp(uv.y * height, 0, height - 1);
    
    int idx = y * width + x;
    float pdf_pixel;
    {
        uint sample_idx = idx;
        pdf_pixel = g_sky_box_pdf[sample_idx];
    }
    float pdf_uv = pdf_pixel * width * height;
    {
        return pdf_pixel / (2 * Pi * Pi * sin(UV2SphereCoord(uv).x));
    }

}


LightLiSample EnvironmentSampling(float2 u2)
{
    uint width = 0, height = 0;
    g_sky_box.GetDimensions(width, height);
    double pdf_pixel;
    uint2 xy;
    // alias sample
    {
        uint num = 0, stride = 0;
        g_sky_box_sampling_prob.GetDimensions(num, stride);

        int idx = u2.x * num;
        idx = (idx == num) ? idx - 1 : idx;
        
        uint sample_idx = idx;
        if (u2.y >= g_sky_box_sampling_prob[idx])
            sample_idx = g_sky_box_sampling_alias[idx];
        
        pdf_pixel = g_sky_box_pdf[sample_idx];
        
        ////
        xy.y = sample_idx / width;
        xy.x = sample_idx - xy.y*width;
        //xy = uint2(sample_idx % width, sample_idx / width);
    }
    
    float u = (xy.x + 0.5f) / width;
    float v = (xy.y + 0.5f) / height;
    float3 wi = UV2SolidAngle(float2(u, v));
    float pdf_uv = pdf_pixel * width * height;
    LightLiSample s = (LightLiSample) 0;
    
    float2 uv = float2(u, v);
    {
        
        s.pdf = pdf_pixel / (2 * Pi * Pi * sin(UV2SphereCoord(uv).x));
    }
    s.to_light = wi;
    s.radiance = g_sky_box.SampleLevel(GetLinearClampSampler(), uv, 0).xyz;
    return s;
}

//LightLiSample EnvironmentSampling2(float2 u2)
//{
//    float2 uv = u2;
//    LightLiSample s;
//    {
//        float theta = (1.0 - uv.y) * Pi;
//        float phi = (0.5 - uv.x) * 2 * Pi;
//        float cosTheta = cos(theta);
//        float sinTheta = sin(theta);
//        float sinPhi = sin(phi);
//        float cosPhi = cos(phi);
//	//left hand coordinate and y is up
//        float x = sinTheta * cosPhi;
//        float y = cosTheta;
//        float z = sinTheta * sinPhi;
//        s.to_light = float3(x, y, z);
//        uint width = 0, height = 0;
//        g_sky_box.GetDimensions(width, height);
//        s.pdf = 1.0 / double(width * height);
//        s.pdf = s.pdf / (2 * Pi * Pi * sinTheta);
//        if (sinTheta == 0)
//        {
//            s.pdf = 0;
//            return (LightLiSample) 0;
//        }
//    }
//    
//    s.radiance = g_sky_box.SampleLevel(GetLinearClampSampler(), uv, 0).xyz;
//    return s;
//}
//
//
//LightLiSample EnvironmentSampling22(float2 u2)
//{
//    LightLiSample s = (LightLiSample) 0;
//    float mapPdf = 0;
//    s.pdf = 0;
//    s.to_light = 0;
//    float2 uv;
//    {
//        uint num = 0, stride = 0;
//        g_sky_box_sampling_prob.GetDimensions(num, stride);
//
//        int idx = u2.x * num;
//        uint sample_idx = idx;
//        if (u2.y > g_sky_box_sampling_prob[idx])
//            sample_idx = g_sky_box_sampling_alias[idx];
//
//        uint width = 0, height = 0;
//        g_sky_box.GetDimensions(width, height);
//        mapPdf = g_sky_box_pdf[sample_idx];
//
//        uv = float2((sample_idx % width + 0.5f) / (float) width, (sample_idx / (float) width + 0.5f) / (float) height);
//    }
//    //if (mapPdf == 0)
//    //{
//    //    s.pdf =0;
//    //    return s;
//    //}
//    float theta = (1.0 - uv.y) * Pi;
//    float phi = (0.5 - uv.x) * 2 * Pi;
//    float cosTheta = cos(theta);
//    float sinTheta = sin(theta);
//    float sinPhi = sin(phi);
//    float cosPhi = cos(phi);
//	//left hand coordinate and y is up
//    float x = sinTheta * cosPhi;
//    float y = cosTheta;
//    float z = sinTheta * sinPhi;
//    s.to_light = float3(x, y, z);
//    // Compute PDF for sampled infinite light direction
//    s.pdf = mapPdf / (2 * Pi * Pi * sinTheta);
//    //EnvironmentPDF(s.to_light);
//    //if (sinTheta == 0)
//    //{
//    //    s.pdf = 0;
//    //    return s;
//    //}
//    s.radiance = g_sky_box.SampleLevel(GetLinearClampSampler(), uv, 0).xyz;
//   // return _LatitudeLongitudeMap.SampleLevel(_LatitudeLongitudeMap_linear_repeat_sampler, uv, 0) * 100;
//
//    
//    //uint num = 0, stride = 0;
//    //g_sky_box_sampling_prob.GetDimensions(num, stride);
//    //
//    //int idx = u2.x * num;
//    //uint sample_idx = idx;
//    //if (u2.y >g_sky_box_sampling_prob[idx])
//    //    sample_idx = g_sky_box_sampling_alias[idx];
//    //
//    //uint width = 0, height = 0;
//    //g_sky_box.GetDimensions(width, height);
//    //float environmental_light_pdf = g_sky_box_pdf[sample_idx];
//    //
//    //float2 uv = float2((sample_idx % width) / (float) width, (sample_idx / (float) width) / (float) height);
//    //
//    //LightLiSample s;
//    //s.to_light = UV2SolidAngle(uv);
//    //s.pdf = EnvironmentPDF(s.to_light);
//    //s.radiance = EnvironmentLe(s.to_light);
//    return s;
//}

//float LightPDF(float3 position_hit,float2 barycentric_coordinate)
//{
//    int light_id;
//    
//    uint lightCount, stride;
//    g_light_buffer.GetDimensions(lightCount, stride);
//    if (lightCount == 0)
//        return 0;
//    
//    PathTracingLight light;
//    for (int i = 0; i < lightCount;i++)
//    {
//        light = g_light_buffer[i];
//        uint3 idx = g_compressed_index_buffer[light.shape_id];
//            
//        float3 p0, p1, p2;
//        p0 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.x].position, 1)).xyz;
//        p1 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.y].position, 1)).xyz;
//        p2 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.z].position, 1)).xyz;
//        
//        
//        float3 sample_point_position1 = barycentric_coordinate.x * p1 + barycentric_coordinate.y * p2 + (1.0 - barycentric_coordinate.x - barycentric_coordinate.y) * p0;
//        float3 sample_point_position1 = barycentric_coordinate.x * p2 + barycentric_coordinate.y * p0 + (1.0 - barycentric_coordinate.x - barycentric_coordinate.y) * p1;
//        float3 sample_point_position1 = barycentric_coordinate.x * p1 + barycentric_coordinate.y * p2 + (1.0 - barycentric_coordinate.x - barycentric_coordinate.y) * p0;
//    }
//}

LightLiSample
    SampleLi(
    float3 shadingPositon, float3 shadingNormal, int lightID, inout RandomSequence
    rndSequence)
{
    LightLiSample res = (LightLiSample) 0;
    PathTracingLight light = g_light_buffer[lightID];
    
    //if(light.shape_id>=0)
    {
        float b1, b2;
        SampleTriangleBarycentricCoordinates(float2(RandomSequence_GenerateSample1D(rndSequence), RandomSequence_GenerateSample1D(rndSequence)), b1, b2);
        
        {
            uint3 idx = g_compressed_index_buffer[light.shape_id];
            
            float3 p0, p1, p2;
            p0 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.x].position, 1)).xyz;            
            p1 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.y].position, 1)).xyz;
            p2 = mul(g_compressed_to_world_buffer[light.to_world_id], float4(g_compressed_vertex_buffer[idx.z].position, 1)).xyz;
           // p0 =g_compressed_vertex_buffer[idx.x].position;
           // p1 = g_compressed_vertex_buffer[idx.y].position;
           //                                               
           // p2 = g_compressed_vertex_buffer[idx.z].position;

            
            float3 sample_point_position = b1 * p1 + b2 * p2 + (1.0 - b1 - b2) * p0;
            float3 sample_point_normal = b1 * g_compressed_vertex_buffer[idx.x].normal + b2 * g_compressed_vertex_buffer[idx.y].normal + (1.0 - b1 - b2) * g_compressed_vertex_buffer[idx.z].normal;
            
            res.pdf = 1.0f / (0.5f * length(cross(p1 - p0, p2 - p0)));
            res.position = sample_point_position;
            res.normal = mul(g_compressed_to_world_buffer[light.to_world_id], float4(sample_point_normal, 0)).xyz;
            //
            //res.normal = sample_point_normal;

        }
        res.radiance = light.radiance_or_irradiance;

    }
   //if (light.IsPointLight())
   //{
   //    PointLight realLight;
   //    realLight.Init(light,s);
   //    return realLight.SampleLi(shadingPositon);
   //}
   //else if (light.IsDistantLight())
   //{
   //    DistantLight realLight;
   //    realLight.Init(light,s);
   //    return realLight.SampleLi(shadingPositon);
   //}
   //else if (light.IsDiffuseAreaLight())
   //{
   //    //res.pdf = 
   //    
   //}
    return res;
}

//int GetClosestLight(RayDesc ray)
//                                                                                                                                                                                                                                                                                                                                                                       
//    uint lightCount, stride;
//    _Light.GetDimensions(lightCount, stride);
//    for (int i = 0; i < lightCount; i++)
//    {
//        PathTracingLight light = _Light[i];
//        if (light.IsDistantLight())
//        {
//            float HitT = ray.TMax;
//            DistantLight_TraceLight(ray, i, HitT);
//        }
//        
//    }
//    return 0;
//}
#endif