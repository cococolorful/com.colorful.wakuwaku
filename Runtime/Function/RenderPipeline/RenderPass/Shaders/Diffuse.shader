Shader"wakuwaku/Diffuse"
{
    Properties
    {
        _AlbedoFactor("_AlbedoFactor", Vector) = (0,0,0,0)
        _AlbedoTex("_AlbedoTex", 2D) = "white" {}

        _EmissiveFactor("_Emissive", Vector) = (0,0,0,0)
        _EmissiveTex("_EmissiveTex", 2D) = "white" {}

    }
        SubShader
        {
            Pass{
                Cull Off

                Name "GBufferPass"
                Tags   {"LightMode" = "GBufferPass"}
                HLSLPROGRAM
                
                #pragma vertex DefaultVS
                #pragma fragment GBufferPS

                #define DIFFUSE
                #include "../ShaderLibrary/DefaultVS.hlsl"
                #include "../ShaderLibrary/GBufferPass.hlsl"

                #pragma enable_d3d11_debug_symbols

                ENDHLSL
            }
            Pass{
                Name "PathTracing"
                Tags   {"LightMode" = "PathTracing"}
                HLSLPROGRAM

                #pragma raytracing test

                #include "../ShaderLibrary/Renderer/Materials/Diffuse.hlsl"
   
                ENDHLSL
            }
        }
     FallBack "Diffuse"
}
