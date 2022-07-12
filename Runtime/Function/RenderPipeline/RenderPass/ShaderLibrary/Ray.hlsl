#ifndef RAY_HLSL
#define RAY_HLSL

// Equivalent to DXR RayDesc,jsut adding some extra member functions
struct Ray
{
    float3 origin;
    float t_min;

    float3 dir;
    float t_max;
    
    #ifdef RAYTRACING
    RayDesc ToRayDesc()
    {
        RayDesc ray = { origin,t_min,dir,t_max };
        //ray.Origin = origin;
        //ray.Direction = dir;
        //ray.TMin = t_min;
        //ray.TMax = t_max;
        return ray;
    }
    #endif    
    float3 Eval(float t)
    {
        return origin + t * dir;
    }
};
#endif