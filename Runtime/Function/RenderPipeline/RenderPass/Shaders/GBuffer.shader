Shader"wakuwaku/Hidden/GBufferRaster"
{
   
    SubShader
    {
        Pass{
            Cull Off

            Name "GBufferPass"
            Conservative True
            HLSLPROGRAM

            #pragma vertex vert                                                                                                           
            #pragma fragment frag
            #pragma target 5.0
            #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            #include "../ShaderLibrary/Scene.hlsl"
            
            struct v2f
            {
                float3 normal_world : NORMAL0;
                float4 tangent_world : TANGENT;
                float2 texcoord : TEXCOORD;
                float3 postion_world : NORMAL1;

                nointerpolation uint instance_id : INSTANCE_ID;
                nointerpolation uint material_id : MATERIAL_ID;

                float4 pos_h : SV_POSITION;
            };


            v2f vert(appdata_tan v,uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                Scene scene;
                v2f o = (v2f)0;
                uint draw_id = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                float4x4 world_matrix = scene.GetWorldMatrix(instanceID);
                float4 pos_w = mul(world_matrix,float4(v.vertex));
                o.postion_world = pos_w.xyz;
                o.pos_h = mul(scene.camera.GetViewProj(),pos_w);
//
                o.texcoord = v.texcoord;
                o.normal_world = normalize(mul(scene.GetInverseTransposeWorldMatrix(instanceID),float4(v.normal,0)).xyz);
                ////o.tangent_world = normal(mul(scene.GetWorldMatrix(instanceID),v.tangent));
//
                o.instance_id = instanceID;
                o.material_id = scene.GetMaterialID(instanceID);
                
                return o;
            }
            
            //struct GBuffer
            //{
            //    float4 wsPos : SV_Target0;
            //    float4 wsNorm : SV_Target1;
            //    float4 matDif : SV_Target2;
            //    float4 matSpec : SV_Target3;
            //    float4 matEmissive : SV_Target4;
            //    float4 linearZAndNormal : SV_Target5;
            //    float4 motionVecFwidth : SV_Target6;
            //};
            struct VBuffer
            {
                float4 data : SV_Target0;
                //int mat_id: SV_Target0;
                //int prim_id : SV_Target1;
                //int instance_id : SV_Target2;
            };
            VBuffer frag(v2f vsOut)
            {
                // ShadingData hitPt = PrepareShadingData(vsOut, _CameraPosW);
                // Dump out our G buffer channels
                VBuffer vBufOut;
                //gBufOut.wsPos = float4(vsOut.postion_world, 1.f);
                //gBufOut.wsNorm = float4(vsOut.normal_world,1.0f);
                
                //gBufOut.matDif = float4(hitPt.diffuse, 1);
                //gBufOut.matSpec = float4(hitPt.specular, hitPt.linearRoughness);
                //gBufOut.matEmissive = float4(hitPt.emissive, 0.f);
                //gBufOut.matDif = _BaseColorFactor;
                //int2 ipos = int2(vsOut.PosH.xy);
                //const float2 pixelPos = ipos + float2(0.5, 0.5);
                //const float4 prevPosH = vsOut.prevPosH;

                //vBufOut.instance_id =int(v2f.instance_id);
                //vBufOut.mat_id = v2f.material_id;
                vBufOut.data = float4(vsOut.instance_id,0,0,1);
                return vBufOut;
            }
            ENDHLSL
        }
            
    }
}
