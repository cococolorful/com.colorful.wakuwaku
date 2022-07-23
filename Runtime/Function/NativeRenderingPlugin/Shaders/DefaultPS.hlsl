#include"VertexDefine.hlsli"
#include"SceneDefine.hlsli"

struct Instance
{
    int instance_id;
    InstanceData GetInstanceData()
    {
        return g_instance_data[instance_id];
    }
};
ConstantBuffer<Instance> ins : register(b1);
SamplerState BilinearSampler : register(s0);
float4 main(DefaultVertexOut v_out) : SV_TARGET
{
    const int mat_id = v_out.material_id;
    //const int mat_id = ins.GetInstanceData().material_id;
    return g_textures[g_materials[mat_id].albedo_idx].Sample(BilinearSampler, v_out.texCoord);
    //return float4(1.0f, 1.0f, 1.0f, 1.0f);
}