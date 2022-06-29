Shader"wakuwaku/EmissiveIntegration"
{
    Properties
    {
        _EmissionTex ("EmissionTex", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Name "EmissiveIntegration"
            Cull Off 
            Conservative True
            ZTest Always
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert                                                                                                           
            #pragma fragment frag
            #pragma target 5.0
            #pragma enable_d3d11_debug_symbols

            
            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            int _triOffset;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation int triOffset : TRIANGLEID;
            };

            sampler2D _EmissionTex;
            float4 __EmissionTex_ST;


            v2f vert(appdata i)
            {
                v2f o;

                float u = i.uv.x, v = i.uv.y;

                // clamp negative u,v ( ideally we should use a target texture that covers some negative uv range )
                if (u < 0.0) u = 0.0;
                if (v < 0.0) v = 0.0;

                while (u > 2.0) u -= 2.0;
                while (v > 2.0) v -= 2.0;

                o.vertex = float4(u - 1, v - 1, 0, 1);
                o.triOffset = _triOffset;
                o.uv = i.uv;
                return o;
            }
            RWStructuredBuffer<uint3> _triangleRadianceList:register(u1);
            RWStructuredBuffer<uint> _triangleNumOfTexels:register(u2);
            float4 frag(v2f i, uint primID : SV_PrimitiveID) : SV_Target
            {
                // sample the texture
                float3 emissiveTexColor =tex2D(_EmissionTex, i.uv).xyz;

                int triId = i.triOffset + int(primID);
                uint org;
               
    {
        InterlockedAdd(_triangleRadianceList[triId].x, uint(clamp(255 * emissiveTexColor.x, 0.f, 255)), org);
        InterlockedAdd(_triangleRadianceList[triId].y, uint(clamp(255 * emissiveTexColor.y, 0.f, 255)), org);
        InterlockedAdd(_triangleRadianceList[triId].z, uint(clamp(255 * emissiveTexColor.z, 0.f, 255)), org);
        //InterlockedAdd(_triangleRadianceList[triId].x, uint(clamp(255 * emissiveTexColor.x, 0.f, 255)), org);
        //InterlockedAdd(_triangleRadianceList[triId].y, uint(clamp(255 * emissiveTexColor.y, 0.f, 255)), org);
        //InterlockedAdd(_triangleRadianceList[triId].z, uint(clamp(255 * emissiveTexColor.z, 0.f, 255)), org);

    }
                InterlockedAdd(_triangleNumOfTexels[triId],1,org);
                return float4(emissiveTexColor,1.0f);
            }
           ENDHLSL
        }
    }
}
