#ifndef ATMOSPHERE_COMMON_HLSL
#define ATMOSPHERE_COMMON_HLSL

#define PI 3.1415926
SamplerState my_point_clamp_sampler;

cbuffer AtmosphereBuffer
{
    float3 scatterRayleigh;
    float hDensityRayleigh;

    float scatterMie;
    float asymmetryMie;
    float absorbMie;
    float hDensityMie;
        
    float3 absorbOzone;
    float ozoneCenterHeight;

    float ozoneThickness;
    float bottomRadius;
    float topRadius;
    float _pad0;
}

struct AtmosphereParameters
{
    float3 scatterRayleigh;
    float hDensityRayleigh;

    float scatterMie;
    float asymmetryMie;
    float absorbMie;
    float hDensityMie;
        
    float3 absorbOzone;
    float ozoneCenterHeight;

    float ozoneThickness;
    float bottomRadius;
    float topRadius;
};

AtmosphereParameters GetAtmosphereParameters()
{
    AtmosphereParameters atmosInfo;

    atmosInfo.scatterRayleigh = scatterRayleigh;
    atmosInfo.hDensityRayleigh = hDensityRayleigh;

    atmosInfo.scatterMie = scatterMie;
    atmosInfo.asymmetryMie = asymmetryMie;
    atmosInfo.absorbMie = absorbMie;
    atmosInfo.hDensityMie = hDensityMie;

    atmosInfo.absorbOzone = absorbOzone;
    atmosInfo.ozoneCenterHeight = ozoneCenterHeight;

    atmosInfo.ozoneThickness = ozoneThickness;
    atmosInfo.bottomRadius = bottomRadius;
    atmosInfo.topRadius = topRadius;

    return atmosInfo;
}

// - r0: ray origin
// - rd: normalized ray direction
// - s0: sphere center
// - sR: sphere radius
// - Returns distance from r0 to first intersecion with sphere,
//   or -1.0 if no intersection.
float raySphereIntersectNearest(float3 r0, float3 rd, float3 s0, float sR)
{
    float a = dot(rd, rd);
    float3 s0_r0 = r0 - s0;
    float b = 2.0 * dot(rd, s0_r0);
    float c = dot(s0_r0, s0_r0) - (sR * sR);
    float delta = b * b - 4.0 * a * c;
    if (delta < 0.0 || a == 0.0)
    {
        return -1.0;
    }
    float sol0 = (-b - sqrt(delta)) / (2.0 * a);
    float sol1 = (-b + sqrt(delta)) / (2.0 * a);
    if (sol0 < 0.0 && sol1 < 0.0)
    {
        return -1.0;
    }
    if (sol0 < 0.0)
    {
        return max(0.0, sol1);
    }
    else if (sol1 < 0.0)
    {
        return max(0.0, sol0);
    }
    return max(0.0, min(sol0, sol1));
}


float3 GetSigmaT(in float3 WorldPos , in AtmosphereParameters atmosphere)
{
    const float viewHeight = length(WorldPos) - atmosphere.bottomRadius;
    float3 rayleigh = atmosphere.scatterRayleigh * exp(-viewHeight / atmosphere.hDensityRayleigh);
    float mie = (atmosphere.scatterMie + atmosphere.absorbMie) * exp(-viewHeight / atmosphere.hDensityMie);
    float3 ozone = atmosphere.absorbOzone * max(
        0.0f, 1 - 2.0 * abs(viewHeight - atmosphere.ozoneCenterHeight) / atmosphere.ozoneThickness);
    return rayleigh + mie + ozone;
}
void GetSigmaST(in float3 WorldPos, in AtmosphereParameters atmosphere, out float3 sigmaS, out float3 sigmaT)
{
    const float viewHeight = length(WorldPos) - atmosphere.bottomRadius;
    float3 rayleigh = atmosphere.scatterRayleigh * exp(-viewHeight / atmosphere.hDensityRayleigh);
    float mieS = (atmosphere.scatterMie) * exp(-viewHeight / atmosphere.hDensityMie);
    float mieA = atmosphere.absorbMie * exp(-viewHeight / atmosphere.hDensityMie);
    float3 ozone = atmosphere.absorbOzone * max(
        0.0f, 1 - 2.0 * abs(viewHeight - atmosphere.ozoneCenterHeight) / atmosphere.ozoneThickness);
    sigmaT =  rayleigh + mieS + mieA + ozone;
    sigmaS = rayleigh + mieS;
}


float3 evalPhaseFunction(float h, float mu, AtmosphereParameters atmosphere)
{
    float3 sRayleigh = atmosphere.scatterRayleigh * exp(-h / atmosphere.hDensityRayleigh);
    float sMie = atmosphere.scatterMie * exp(-h / atmosphere.hDensityMie);
    float3 s = sRayleigh + sMie;

    float g = atmosphere.asymmetryMie, g2 = g * g, u2 = mu * mu;
    float pRayleigh = 3 / (16 * PI) * (1 + u2);

    float m = 1 + g2 - 2 * g * mu;
    float pMie = 3 / (8 * PI) * (1 - g2) * (1 + u2) / ((2 + g2) * m * sqrt(m));

    float3 result;
    result.x = s.x > 0 ? (pRayleigh * sRayleigh.x + pMie * sMie) / s.x : 0;
    result.y = s.y > 0 ? (pRayleigh * sRayleigh.y + pMie * sMie) / s.y : 0;
    result.z = s.z > 0 ? (pRayleigh * sRayleigh.z + pMie * sMie) / s.z : 0;
    return result;
}

float ClampCosine(float mu)
{
    return clamp(mu, float(-1.0), float(1.0));
}
float ClampDistance(float d)
{
    return max(d, 0.0f);
}
float SafeSqrt(float a)
{
    return sqrt(max(a, 0.0f));
}

float DistanceToTopAtmosphereBoundary(AtmosphereParameters atmosphere, float r, float mu)
{
    float discriminant = r * r * (mu * mu - 1.0f) + atmosphere.topRadius * atmosphere.topRadius;

    return ClampDistance(-r * mu + SafeSqrt(discriminant));
}
float DistanceToBottomAtmosphereBoundary(AtmosphereParameters atmosphere, float r, float mu)
{
    float discriminant = r * r * (mu * mu - 1.0f) + atmosphere.bottomRadius * atmosphere.bottomRadius;

    return ClampDistance(-r * mu - SafeSqrt(discriminant));
}
bool RayIntersectsGround(AtmosphereParameters atmosphere, float r, float mu)
{
    return mu < 0.0 && r * r * (mu * mu - 1.0) +
      atmosphere.bottomRadius * atmosphere.bottomRadius >= 0.0;
}
// x in [0,1] to u in [0.5/n,1-0.5/n]
float GetTextureCoordFromUnitRange(float x, int textureSize)
{
    return 0.5f / float(textureSize) + x * (1.0f - 1.0f / float(textureSize));
}
float GetUnitRangeFromTextureCoord(float u, int textureSize)
{
    return (u - 0.5f / float(textureSize)) / (1.0f - 1.0f / float(textureSize));
}
float fromUnitToSubUvs(float u, float resolution)
{
    return (u + 0.5f / resolution) * (resolution / (resolution + 1.0f));
}
float fromSubUvsToUnit(float u, float resolution)
{
    return (u - 0.5f / resolution) * (resolution / (resolution - 1.0f));
}

void GetTransmittanceTextureUVFromRMu(AtmosphereParameters atmosphere, float viewHeight, float viewZenithCosAngle ,out float2 uv)
{
    float H = sqrt(atmosphere.topRadius * atmosphere.topRadius - atmosphere.bottomRadius * atmosphere.bottomRadius);
    float rho = SafeSqrt(viewHeight * viewHeight - atmosphere.bottomRadius * atmosphere.bottomRadius);

    float d = DistanceToTopAtmosphereBoundary(atmosphere, viewHeight, viewZenithCosAngle);
    float dMax = rho + H;
    float dMin = atmosphere.topRadius - viewHeight;

    float xMu = (d - dMin) / (dMax - dMin);
    float xR = rho / H;

    uv =  float2(xMu, xR);
    //return float2(GetTextureCoordFromUnitRange(xMu, textureSize.x), GetTextureCoordFromUnitRange(xR, textureSize.y));
}
void GetTransmittanceTextureRMuFromUV(AtmosphereParameters atmosphere, float2 uv, out float viewHeight, out float viewZenithCosAngle)
{
    float x_mu = uv.x;
    float x_r = uv.y;
    
    float H = sqrt(atmosphere.topRadius * atmosphere.topRadius - atmosphere.bottomRadius * atmosphere.bottomRadius);
    float rho = H * x_r;
    viewHeight = SafeSqrt(rho * rho + atmosphere.bottomRadius * atmosphere.bottomRadius);

    float d_min = atmosphere.topRadius - viewHeight;
    float d_max = rho + H;
    float d = d_min + x_mu * (d_max - d_min);
    viewZenithCosAngle = d == 0.0 ? 1.0f : (H * H - rho * rho - d * d) / (2.0 * viewHeight * d);
    viewZenithCosAngle = clamp(viewZenithCosAngle, -1.0, 1.0);
    
}

float3 GetTransmittance(AtmosphereParameters atmosphere, Texture2D transmittance, float viewHeight, float viewZenithCosAngle)
{
    float2 uv;
    GetTransmittanceTextureUVFromRMu(atmosphere, viewHeight, viewZenithCosAngle, uv);
    return transmittance.SampleLevel(my_point_clamp_sampler,uv,0).xyz;
}
#endif