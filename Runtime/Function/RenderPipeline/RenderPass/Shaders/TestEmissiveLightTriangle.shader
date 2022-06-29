Shader"wakuwaku/TestEmissiveLightTriangle"
{
    Properties
    { 
    }
    SubShader
    { 
        Pass{
                Name "TestEmissiveLightTriangle"
                HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                
                #pragma enable_d3d11_debug_symbols

                #include "../ShaderLibrary/Input.hlsl"
                #pragma target 5.0
                
                struct VertexOut
                {
                    float4 PosH : SV_POSITION;
                    float3 TexC : TEXCOORD;
                };
                float4x4 L2W;
                int Offset ;
                struct EmissiveVertex
                {
                    float3 position;
                    float3 normal;
                };
                StructuredBuffer<EmissiveVertex> VertexBuffer ;
                StructuredBuffer<uint> IndexBuffer ;
                StructuredBuffer<float> FluxBuffer ;
                
                
                VertexOut vert(uint VertID : SV_VertexID)
                {
                    VertID += Offset;
                    VertexOut vout;

                    vout.PosH = mul(GetCameraProj(), mul(GetCameraView(), mul(L2W,float4(VertexBuffer[IndexBuffer[VertID]].position,1.0f))));
                    vout.TexC.x = FluxBuffer[(int)VertID / 3];
                    //vout.TexC = gPositions[gIndexMap[VertID]];

                    return vout;
                }

                float4 frag(VertexOut pin) : SV_Target
                {
                    return float4(pin.TexC.x,pin.TexC.x,pin.TexC.x,1);
                }
                ENDHLSL
            }

    }
    FallBack "Diffuse"
}
