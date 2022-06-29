Shader"wakuwaku/Skybox"
{
    Properties
    {
        _SkyboxTex("SkyboxTex", Cube) = "white" {}
    }
        SubShader
        {
           Pass{
                
                Name "SkyboxPass"
                HLSLPROGRAM

                #pragma vertex SkyboxVS
                #pragma fragment SkyboxPS

                #include "../ShaderLibrary/SkyboxPass.hlsl"

                #pragma enable_d3d11_debug_symbols


                ENDHLSL
            }
        }
            FallBack "Diffuse"
}
