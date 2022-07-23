#include "VertexDefine.hlsli"
#include "SceneDefine.hlsli"


struct Instance
{
    int instance_id;
    InstanceData GetInstanceData()
    {
        return g_instance_data[instance_id];
    }
};
ConstantBuffer<Instance> ins : register(b1);

DefaultVertexOut main(DefaultVertexIn v_in) 
{
    const InstanceData ins_data = ins.GetInstanceData();
    
    DefaultVertexOut o;
    float4x4 worldMat = ins_data.GetWorldMatrix();
    float4 posW = mul(worldMat, float4(v_in.PosL, 1.0f));
    o.PosW = posW.xyz;
    o.PosH = mul(g_camera.CameraGetViewProj(), posW);
    
    o.texCoord = v_in.texCoord;
    o.NormalW = normalize(mul(ins_data.GetWorldInvTransMatrix(), float4(v_in.NormalL, 0))).xyz;
    o.TangentW = normalize(mul(ins_data.GetWorldMatrix(), v_in.TangentL));
    float4 prePos = float4(v_in.PosL, 1.0f);
    float4 prePosW = mul(ins_data.GetPrevWorldMatrix(), prePos);
    o.prevPosH = mul(g_camera.CameraGetPervViewProj(), prePosW);
    
    o.instance_id = ins.instance_id;
    o.material_id = ins_data.material_id;
    return o;
}