#ifndef METAL_WORKFLOW_HLSL
#define METAL_WORKFLOW_HLSL

#include "MaterialCommon.hlsl"
#include "../SampleTransformUtil.hlsl"

float DTrowbridgeReitz(float3 half_vector, float roughness)
{
    float alpha2 = Pow2(Pow2(roughness));
    float t = 1 + (alpha2 - 1) * Pow2(half_vector.z);
    //return alpha2 / (M_PI * Pow2(Pow2(half_vector.z) * (alpha2 - 1) + 1) + 1e-6f);
    return alpha2 / (M_PI * Pow2(t) + 1e-6f);
}
float3 FSchlick(float3 f0,float3 wi,float3 half_vector)
{
    float cosine_theta = dot(wi, half_vector);
    return f0 + (1.0 - f0) * Pow5(1.0 - cosine_theta);
}
float GGGX(float3 wi,float3 wo,float roughness)
{
    if (wo.z <= 0 || wi.z <= 0)
        return 0;
    
    // k = alpha / 2
    // direct light: alpha = pow( (roughness + 1) / 2, 2)
    // IBL(image base lighting) : alpha = pow( roughness, 2)

    
    float k = Pow2(roughness + 1) / 8.0;
    float g_wo = wo.z / (wo.z * (1 - k) + k);
    float g_wi = wi.z / (wi.z * (1 - k) + k);
    return g_wo * g_wi;
}
class MetalWorkflowBRDF
{
    Spectrum diffuse_;
    Spectrum specular_;
    
    float roughness_;
    
    void Init(Spectrum base_color, float metallic, float roughness)
    {
        diffuse_ = base_color * saturate((1.0 - metallic));
        specular_ = lerp(0.04, base_color, metallic);
        
        // TODO:粗糙度很小的时候能量损失好像很大
        roughness_ = max(saturate(roughness),0.1f);
    }
    Spectrum Eval(float3 wo, float3 wi)
    {
        if (!SameHemisphere(wo,wi))
            return 0;
        
        // if EffectivelySmooth
        float cos_theta_o = AbsCosineTheta(wo), cos_theta_i = AbsCosineTheta(wi);
        if (cos_theta_o == 0 || cos_theta_i == 0)
            return 0;
        
        float3 half_vector = (wo + wi);
        if (length(half_vector) == 0)
            return 0;
        half_vector = normalize(half_vector);
        
        Spectrum ks = FSchlick(specular_, wo, half_vector);
        Spectrum specular_item = ks * DTrowbridgeReitz(half_vector, roughness_) * GGGX(wi, wo, roughness_) / (4.0 * wi.z * wo.z + 1e-6f);
        Spectrum diffuse_item = diffuse_ * InvPi* (1.0 - ks);
        //if (any(isnan(diffuse_item)))
        //    return float3(0, 0, 0);
        //return diffuse_item;
        //return specular_item;
        return specular_item + diffuse_item;
    }
    float PDF(float3 wo, float3 wi)
    {
        if (!SameHemisphere(wo, wi))
            return 0;
        float3 half_vector = normalize(wo + wi);

        return DTrowbridgeReitz(half_vector, roughness_) / 4.0f;
    }
    
    BSDFSample Sample(float3 wo, float2 u)
    {
        BSDFSample result;
        
       //result.wi = SampleCosineHemisphere(u);
       //result.f = Eval(wo,result.wi);
       //result.pdf = CosineHemispherePDF(AbsCosineTheta(result.wi));
        
        float alpha = Pow2(roughness_);
        float cos_theta_2 = (1.0 - u.x) / (u.x * (Pow2(alpha) - 1) + 1);
        float cos_theta = sqrt(cos_theta_2);
        float sin_theta = sqrt(1 - cos_theta_2);
        float phi = 2 * M_PI * u.y;
        float3 h = float3(sin_theta * cos(phi), sin_theta * sin(phi), cos_theta);
        result.wi= reflect(-wo, h);
        if(result.wi.z<=0)
        {
            result.pdf= 0;
            
        }else
        {
            result.pdf = PDF(wo,result.wi);
            result.f = Eval(wo, result.wi);
        }
        return result;
    }
    
};

#include "../RTCommon.hlsl"
#include "MaterialCommon.hlsl"
#include "../Lights/LightSampling.hlsl"
#include "../SceneUtil.hlsl"
#include "../../Input.hlsl"
#include "../Unity/UnityRaytracingMeshUtils.cginc"
            
    
Texture2D _EmissiveTex;
float3 _EmissiveFactor;

Texture2D _BaseColorTex;
float3 _BaseColorFactor;

Texture2D _OcclusionMetallicRoughnessTexture;
float3 _OcclusionMetallicRoughnessFactor;

[shader("closesthit")]
void ClosestHitShader(inout PathVertexPayload vertexPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    vertexPayload.hit_environment_ = false;
    if (vertexPayload.IsNormalTest())
    {
        NormalTexCoord shading_point = GetShadingPoint(attributeData);
        
        MetalWorkflowBRDF currentBXDF;
        
        Spectrum base_color = _BaseColorFactor * _BaseColorTex.SampleLevel(GetLinearClampSampler(), shading_point.texCoord, 0).xyz;
        float3 occlusion_metallic_roughness = _OcclusionMetallicRoughnessFactor * _OcclusionMetallicRoughnessTexture.SampleLevel(GetLinearClampSampler(), shading_point.texCoord, 0).xyz;
        currentBXDF.Init(base_color, occlusion_metallic_roughness.y, occlusion_metallic_roughness.z);
        
        //if (dot(shading_point.shadingFrame.Normal(), -WorldRayDirection()) < 0)
        //    shading_point.shadingFrame.Filp();
        
        vertexPayload.SetShadingPoint(shading_point);
        
        float3 wo = shading_point.shadingFrame.ToLocal(-WorldRayDirection());
        vertexPayload.BRDFSamplingResult = currentBXDF.Sample(wo, RandomSequence_GenerateSample2D(vertexPayload.RndSequence));
        //shading_point.shadingFrame.Filp();
        vertexPayload.BRDFSamplingResult.wi = normalize(shading_point.shadingFrame.ToWorld(vertexPayload.BRDFSamplingResult.wi));

        // direct light estimation is made up of area light importance sampling
        // and environment texture importance sampling
        vertexPayload.DirectLightEstimation = (Spectrum) 0; 
        
        // area light importance sampling
        {
            LightSample sampleRes = SampleLights(vertexPayload.RndSequence);
            if (sampleRes.pdf > 0)
            {
                LightLiSample area_light_samping_res = SampleLi(vertexPayload.Position, vertexPayload.Normal, sampleRes.lightID, vertexPayload.RndSequence);
                area_light_samping_res.pdf *= sampleRes.pdf;
               
                if (area_light_samping_res.pdf > 0)
                {
                    float3 wiRender = area_light_samping_res.position - vertexPayload.Position;
                    float lenth_to_light = length(wiRender);
                    wiRender = wiRender / lenth_to_light;
                
                    wiRender = normalize(wiRender);
                
                    float vItem = TraceVisibilityRay(vertexPayload.Position, vertexPayload.Normal, wiRender, lenth_to_light);
                  
                    float3 wi = shading_point.shadingFrame.ToLocal(wiRender);
                    Spectrum f = currentBXDF.Eval(wo, wi);
                
                    float g_item = vItem * SafeDot(-wiRender, area_light_samping_res.normal) / Pow2(lenth_to_light);
                    
                    float area_light_pdf_in_solid = area_light_samping_res.pdf / g_item;
                    float brdf_pdf = currentBXDF.PDF(wo, wi);
                    float environmental_light_pdf = 0;
                    float mis_wight = PowerHeuristic(1, area_light_pdf_in_solid, 1, brdf_pdf, 1, environmental_light_pdf); // / (area_light_samping_res.pdf + currentBXDF.PDF(wo, wi) * g_item);
        
                    vertexPayload.DirectLightEstimation += mis_wight * area_light_samping_res.radiance * f * wi.z / (area_light_pdf_in_solid);;
                
                }
            }
        }
        
        // environment texture importance sampling
        {
            LightLiSample env_res = EnvironmentSampling(RandomSequence_GenerateSample2D(vertexPayload.RndSequence));
            
            float3 wi = shading_point.shadingFrame.ToLocal(normalize(env_res.to_light));
            
            float vItem = TraceVisibilityRay(vertexPayload.Position, vertexPayload.Normal, env_res.to_light, 1e16f);
                  
            
            Spectrum f = currentBXDF.Eval(wo, wi);
            
            float environmental_light_pdf = env_res.pdf;
            float area_light_pdf_in_solid = 0;
            float brdf_pdf = currentBXDF.PDF(wo, wi);
            float mis_wight = PowerHeuristic(1, environmental_light_pdf, 1, brdf_pdf, 1, area_light_pdf_in_solid);

            float3 ra = 10*
            vItem*mis_wight * env_res.radiance * f * wi.z / env_res.pdf;;
            //if (any(isnan(ra)))
            //    ra = float3(111110, 0, 0);
           //if (env_res.pdf == 0.0)
           //    ;// ra = float3(0, 0, 1111110);
            vertexPayload.DirectLightEstimation += vItem * mis_wight * env_res.radiance * f * wi.z / env_res.pdf;

            
            //vertexPayload.DirectLightEstimation = vItem  * env_res.radiance * f * wi.z / env_res.pdf;;
            //vertexPayload.DirectLightEstimation = vItem * env_res.radiance * f * wi.z / env_res.pdf;
            //if (env_res.pdf == 0)
            //    vertexPayload.DirectLightEstimation = float3(0, 0, 1111111);
            //vertexPayload.DirectLightEstimation = env_res.radiance;
            //vertexPayload.DirectLightEstimation = vItem * env_res.radiance ;
            //vertexPayload.DirectLightEstimation = env_res.to_light; //            mis_wight *  * f * wi.z / env_res.pdf;;
           // if (abs(length(vertexPayload.DirectLightEstimation - float3(1.27344, 1.3821, 1.5625))) < 1e3f)
           // {
           //     vertexPayload.DirectLightEstimation = float3(0, 1111111, 0);
           // 
           // }
           // if (abs(length(env_res.normal.xy - float2(4096, 2048))) < 1e3f)
           // {
           //     vertexPayload.DirectLightEstimation = float3(0,0, 1111111);
           // 
           // }
           // vertexPayload.DirectLightEstimation = float3(env_res.normal.xy, 0);
            //vertexPayload.DirectLightEstimation = float3(2459, 867, 0);
        }

    }
   else if (vertexPayload.IsVisibility())
   {
       // vertexPayload.SetMiss();

    }

}
             
#endif