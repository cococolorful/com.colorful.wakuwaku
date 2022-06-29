Shader "wakuwaku/MetalWorkFlow"
{
    Properties
    {
        _BaseColorTex("_BaseColorTex", 2D) = "white" {}
        _BaseColorFactor("_BaseColorFactor", Vector) = (0,0,0,0)

        _OcclusionMetallicRoughnessTexture("_OcclusionMetallicRoughnessTexture", 2D) = "white" {}
        _OcclusionMetallicRoughnessFactor("_OcclusionMetallicRoughnessFactor", Vector) = (0,0,0,0)

        _NormalMap("_NormalMap", 2D) = "white" {}

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

                #include "../ShaderLibrary/Renderer/Materials/MetalWorkflow.hlsl"
   
                ENDHLSL
            }
        }
     FallBack "Diffuse"
}
