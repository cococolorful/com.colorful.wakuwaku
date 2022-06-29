#ifndef SAMPLE_TRANSFORM_UTIL_HLSL
#define SAMPLE_TRANSFORM_UTIL_HLSL
#include "RTCommon.hlsl"


float PowerHeuristic(int n_f, float pdf_f, int n_g, float pdf_g)
{
    float f = n_f * pdf_f, g = n_g * pdf_g;
    if (isinf(sqrt(f)))
        return 1;
    return sqrt(f) / (sqrt(f) + sqrt(g));
}
float PowerHeuristic(int n_f, float pdf_f, int n_g, float pdf_g, int n_h,float pdf_h)
{
    float f = n_f * pdf_f, g = n_g * pdf_g,h = n_h * pdf_h;
    if (isinf(sqrt(f)))
        return 1;
    return sqrt(f) / (sqrt(f) + sqrt(g) + sqrt(h));
}

float2 SampleUniformDiskConcentric(float2 u,float radiusDisk = 1.0f)
{
    // map _u_ to [-1,1]
    //float2 uOfffset = 2*u - 1;
    
    float a = 2 * u.x - 1;
    float b = 2 * u.y - 1;
    
    float theta, r;
    if(a*a < b*b)
    {
       // r = radiusDisk * uOfffset.x;
        r = a;
        theta = PiOver4 * (b / a);
    }
    else
    {
        r = b;
        theta = PiOver2 - PiOver4 * (a / b);
    }
    return r * float2(cos(theta), sin(theta));
}

float3 SampleCosineHemisphere(float2 u2)
{
    float radius = sqrt(u2.x);
    float phi = 2 * M_PI * u2.y;
    float x = radius * cos(phi);
    float y = radius * sin(phi);
    float z = sqrt(1.0f - u2.x);
    //float pdf = z / M_PI;
    return float3(x, y, z);
    
    // wrong
   // float2 disk = SampleUniformDiskConcentric(u2);
   // float z1 = SafeSqrt(1.0 - u2.x);
   // return float3(disk.x, disk.y, z1);
}
float CosineHemispherePDF(float cosTheta)
{
    return cosTheta * InvPi;
}
float3 SampleCosineHemisphere(float2 u,float3 normal)
{
    float a = 1 - 2 * u.x;
    float b = sqrt(1 - a * a);
    float phi = 2 * M_PI * u.y;
    return normalize(normal + float3(b * cos(phi), b * sin(phi), a));
}

// Given 2d uniform, outputs the barycentric coordinate of the sample point
// https://cseweb.ucsd.edu/~tzli/cse272/lectures/triangle_sampling.pdf
void SampleTriangleBarycentricCoordinates(float2 u2,out float b1,out float b2)
{
    b1 = 1.0 - sqrt(u2.x);
    b2 = (1.0 - b1) * u2.y;
}
#endif