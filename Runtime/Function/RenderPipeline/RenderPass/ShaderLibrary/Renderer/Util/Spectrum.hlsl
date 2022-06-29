#ifndef UTIL_SEPCTRUM_HLSL
#define UTIL_SEPCTRUM_HLSL
#include "../RTCommon.hlsl"

#define Spectrum float3

Float Blackbody(Float lambda, Float T)
{
#define __BlackbodyC 299792458.f
#define __BlackbodyH 6.62606957e-34f
#define __BlackbodyKB 1.3806488e-23f
    if (T <= 0)
        return 0;
    // Return emitted radiance for blackbody at wavelength _lambda_
    Float l = lambda * 1e-9f;
    Float Le = (2 * __BlackbodyH * __BlackbodyC * __BlackbodyC) / (Pow5(l) * (exp((__BlackbodyH * __BlackbodyC) / (l * __BlackbodyKB * T)) - 1));
    if(isnan(Le))
        Le = -10;
    return Le;
}

#endif