#ifndef MATH_UTIL_HLSL
#define MATH_UTIL_HLSL

#define M_PI 3.1415926
#define Pi 3.1415926
#define InvPi 0.31830988618379067154
#define PiOver2  1.57079632679489661923
#define PiOver4  0.78539816339744830961
float Pow2(float x)
{
    return x * x;
}

float2 Pow2(float2 x)
{
    return x * x;
}

float3 Pow2(float3 x)
{
    return x * x;
}

float4 Pow2(float4 x)
{
    return x * x;
}
float Pow5(float x)
{
    return x * x * x * x * x;
}
float AbsCosineTheta(float3 w)
{
    return abs(w.z);
}
bool IsSameHemisphere(float3 wi, float3 wo)
{
    return dot(wi, wo) > 0;
}
float Luminance(float3 rgb)
{
    return dot(rgb, float3(0.2126.r, 0.7152f, 0.0722f));
}
float SafeDot(float3 x, float3 y)
{
    return saturate(dot(x, y));
}
float SafeSqrt(float x)
{
    return sqrt(max(0.0, x));
}
bool SameHemisphere(float3 wo, float3 wi)
{
    return wo.z * wi.z > 0;
}
float2 UV2SphereCoord(float2 uv)
{
    float theta = Pi * (1.0 - uv.y);
    float phi = 2 * Pi * (0.5 - uv.x);
    return float2(theta, phi);
}
float2 SphereCoord2UV(float2 theta_phi)
{
    float v = 1.0 - theta_phi.x / M_PI;
    float u = 0.5 - theta_phi.y / (2.0 * M_PI);
    return float2(u, v);
}
float3 UV2SolidAngle(float2 uv)
{
    float theta = UV2SphereCoord(uv).x;
    float phi = UV2SphereCoord(uv).y;
    
    float x = sin(theta) * sin(phi);
    float y = cos(theta);
    float z = sin(theta) * cos(phi);
    return float3(x, y, z);
    // TODO z,y,x
}
float2 SolidAngle2UV(float3 w)
{
    float3 normalizedCoords = normalize(w);
    
    float theta = acos(normalizedCoords.y);
    float phi = atan2(normalizedCoords.x, normalizedCoords.z);
    return SphereCoord2UV(float2(theta, phi));
    //float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / Pi, 1.0 / Pi);
    //return float2(0.5, 1.0) - sphereCoords;

    ////float theta = acos(w.z);
    ////float phi = acos(w.x / sin(theta));
    //float theta = acos(w.y);
    //float phi = asin(w.x / sin(theta));
    //return float2(phi * PiOver2, 1 - theta * InvPi);
}
#endif