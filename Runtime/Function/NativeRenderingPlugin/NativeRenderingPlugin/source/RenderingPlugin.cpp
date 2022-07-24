// Example low level rendering Unity plugin

#include "Function/Graphics/Unity/IUnityInterface.h"
#include "Function/Graphics/Unity/IUnityGraphics.h"
#include "Function/Graphics/GraphicsCore.h"

#include "Core/LogSystem.h"
#include <assert.h>
#include <math.h>
#include <vector>
#include "Function/Graphics/Command/CommandContext.h"
#include "Function/Graphics/Resource/ColorBuffer.h"
#include <unordered_map>
#include "Function/Graphics/Pipeline/SamplerManager.h"
#include "CompiledShaders/ScreenQuadVS.h"
#include "CompiledShaders/BlitPS.h"
#include "CompiledShaders/DefaultVS.h"
#include "CompiledShaders/DefaultPS.h"
#include "Function/Graphics/Resource/DepthBuffer.h"


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API 
	RegisterLog(
		wakuwaku::LogManager::LogFunc log_info, 
		wakuwaku::LogManager::LogFunc log_warning, 
		wakuwaku::LogManager::LogFunc log_error)
{
	wakuwaku::LogManager::Instance().Initialize(log_info, log_warning, log_error);
}

class Material
{
public:

};
namespace wakuwaku
{
	void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
	// Unity related
	IUnityInterfaces* s_UnityInterfaces = NULL;
	IUnityGraphics* s_Graphics = NULL;

	class NativeRenderer
	{
		using MaterialInstanceID = int;
	public:
		void Initialize();
		
		void Shutdown();

		
		constexpr static DXGI_FORMAT k_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
		// Rendering
		size_t scene_width;
		size_t scene_height;
		DXGI_FORMAT scene_format;

		int frame_buffer_count;
		int current_frame_buffer_idx;
		std::vector< ColorBuffer> frame_buffers = std::vector< ColorBuffer>(frame_buffer_count);
		DepthBuffer scene_depth_buffer = {0.0f,0 };


		void OnSize(int width, int height, int frame_buffer_count, DXGI_FORMAT format);
		void Render();

		// scene manipulate
		struct Mesh
		{
			StructuredBuffer vectex_buffer;
			StructuredBuffer index_buffer;
			struct SubMesh
			{
				int index_start;
				int index_count;
			};
			std::vector<SubMesh> subMeshes;
		};
		std::unordered_map<int, Mesh> mesh_map;

		struct RenderItem
		{
			int mesh_id;
			Math::Matrix4 transform;
			std::vector<MaterialInstanceID> material;
		};
		std::vector<RenderItem> temp_render_items;
		int AddMesh(int mesh_id, void* vertex_handle, int num_vectex_elements, void* index_handlem, int num_index_elements, Mesh::SubMesh* subMeshes, int subMesh_count);


		// texture
		struct TextureHandle
		{
			int texture_id;
			Texture texture;
		};
		std::unordered_map<int, TextureHandle> texture_map;
		int AddTexture(int id, void* texture_hanle);


		// -------------------------------------------------------------------------------------------
		// Camera
		// -------------------------------------------------------------------------------------------
		struct CameraData
		{
			Math::Matrix4 view_matrix;
			Math::Matrix4 prev_view_matrix;
			Math::Matrix4 proj_matrix;
			Math::Matrix4 view_proj_matrix;
			Math::Matrix4 inv_view_proj_matrix;
			Math::Matrix4 view_proj_no_jitter_matrix;
			Math::Matrix4 prev_view_proj_no_jitter_matrix;
			Math::Matrix4 jitter_matrix;

			Math::XMFLOAT3 pos_world;
			float near_z;

			Math::XMFLOAT3 right;
			float jitter_x;

			Math::XMFLOAT3 up;
			float jitter_y;

			Math::XMFLOAT3 forward;
			float far_z;
		};

		CameraData _camera;
		// -------------------------------------------------------------------------------------------
		// Scene data
		// -------------------------------------------------------------------------------------------
		std::vector<Math::Matrix4> world_matries;
		std::vector<Math::Matrix4> prev_world_matries;
		std::vector<Math::Matrix4> world_inv_transpose_matries;
		
		struct Material
		{
			int albedo_idx;
		};
		std::vector<Material> materials;
		
		std::unordered_map<MaterialInstanceID, int> material_map;

		struct InstanceDataGPU
		{
			int transform_id;
			int material_id;
		};
		std::vector<InstanceDataGPU> instance_gpu_datas;
	
		struct InstanceDataCPU
		{
			int mesh_id;
			int subMesh_id;
		}; 
		std::vector<InstanceDataCPU> instance_cpu_datas;
	};
};

std::unique_ptr< wakuwaku::NativeRenderer> g_native_renderer;

namespace wakuwaku::GraphicsState
{
	SamplerDesc SamplerLinearClampDesc;
	SamplerDesc SamplerLinearWarpDesc;
	
	D3D12_RASTERIZER_DESC RasterizerDefault; // counter-clockwise
	D3D12_RASTERIZER_DESC RasterizerDefaultCw; // clockwise

	D3D12_BLEND_DESC BlendNoColorWrite;
	D3D12_BLEND_DESC BlendTransparent;

	D3D12_DEPTH_STENCIL_DESC DepthStateDefault;
	D3D12_DEPTH_STENCIL_DESC DepthStateDisabled;

	void Initialize();
}	
void wakuwaku::GraphicsState::Initialize()
{
	SamplerLinearClampDesc.Filter = D3D12_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR;
	SamplerLinearClampDesc.SetTextureAddressMode(D3D12_TEXTURE_ADDRESS_MODE_CLAMP);

	SamplerLinearWarpDesc.Filter = D3D12_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR;
	SamplerLinearWarpDesc.SetTextureAddressMode( D3D12_TEXTURE_ADDRESS_MODE_WRAP);
	// --------------------------------------------------------------------------------------
	// rasterizer states
	// --------------------------------------------------------------------------------------
	RasterizerDefault.FillMode = D3D12_FILL_MODE_SOLID;
	RasterizerDefault.CullMode = D3D12_CULL_MODE_BACK;
	RasterizerDefault.FrontCounterClockwise = true;
	RasterizerDefault.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
	RasterizerDefault.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
	RasterizerDefault.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
	RasterizerDefault.DepthClipEnable = TRUE;
	RasterizerDefault.MultisampleEnable = FALSE;
	RasterizerDefault.AntialiasedLineEnable = FALSE;
	RasterizerDefault.ForcedSampleCount = 0;
	RasterizerDefault.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;

	RasterizerDefaultCw = RasterizerDefault;
	RasterizerDefaultCw.FrontCounterClockwise = FALSE;

	// --------------------------------------------------------------------------------------
	// blend states
	// --------------------------------------------------------------------------------------
	
	// Color = SrcAlpha * SrcColor + (1 - SrcAlpha) * DestColor 
	// Alpha = SrcAlpha
	
	BlendTransparent.IndependentBlendEnable = FALSE;
	BlendTransparent.RenderTarget[0].BlendEnable = TRUE;
	BlendTransparent.RenderTarget[0].SrcBlend = D3D12_BLEND_SRC_ALPHA;
	BlendTransparent.RenderTarget[0].DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
	BlendTransparent.RenderTarget[0].BlendOp = D3D12_BLEND_OP_ADD;
	BlendTransparent.RenderTarget[0].SrcBlendAlpha = D3D12_BLEND_ONE;
	BlendTransparent.RenderTarget[0].DestBlendAlpha = D3D12_BLEND_INV_SRC_ALPHA;
	BlendTransparent.RenderTarget[0].BlendOpAlpha = D3D12_BLEND_OP_ADD;
	BlendTransparent.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;

	BlendNoColorWrite = BlendTransparent;
	BlendNoColorWrite.RenderTarget[0].BlendEnable = FALSE;
	BlendNoColorWrite.RenderTarget[0].RenderTargetWriteMask = 0;
	

	// --------------------------------------------------------------------------------------
	// depth stencil states
	// --------------------------------------------------------------------------------------
	DepthStateDisabled.DepthEnable = FALSE;
	DepthStateDisabled.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ZERO;
	DepthStateDisabled.DepthFunc = D3D12_COMPARISON_FUNC_ALWAYS;
	DepthStateDisabled.StencilEnable = FALSE;
	DepthStateDisabled.StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
	DepthStateDisabled.StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;
	DepthStateDisabled.FrontFace.StencilFunc = D3D12_COMPARISON_FUNC_ALWAYS;
	DepthStateDisabled.FrontFace.StencilPassOp = D3D12_STENCIL_OP_KEEP;
	DepthStateDisabled.FrontFace.StencilFailOp = D3D12_STENCIL_OP_KEEP;
	DepthStateDisabled.FrontFace.StencilDepthFailOp = D3D12_STENCIL_OP_KEEP;
	DepthStateDisabled.BackFace = DepthStateDisabled.FrontFace;

	DepthStateDefault = {};
	DepthStateDefault.DepthEnable = TRUE;
	DepthStateDefault.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
	DepthStateDefault.DepthFunc =  D3D12_COMPARISON_FUNC_GREATER_EQUAL;

}
namespace wakuwaku::PSOManager
{
	RootSignature rs_present;
	GraphicsPSO pso_blit;

	RootSignature rs_default;
	GraphicsPSO pso_default;
	void Initialize();
}
void wakuwaku::PSOManager::Initialize()
{
	rs_present.Reset(1, 1);
	CD3DX12_RASTERIZER_DESC s;
	rs_present[0].InitAsDescriptorRange(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 0, 1);
	rs_present.InitStaticSampler(0, GraphicsState::SamplerLinearClampDesc);
	rs_present.Finalize(L"present");

	pso_blit.SetRootSignature(rs_present);
	pso_blit.SetRasterizerState(GraphicsState::RasterizerDefault);
	pso_blit.SetBlendState(GraphicsState::BlendTransparent);
	pso_blit.SetDepthStencilState(GraphicsState::DepthStateDisabled);
	pso_blit.SetSampleMask(0xFFFFFFFF);
	pso_blit.SetInputLayout(0, nullptr);
	pso_blit.SetPrimitiveTopologyType(D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE);
	pso_blit.SetVertexShader(g_pScreenQuadVS, sizeof(g_pScreenQuadVS));
	pso_blit.SetPixelShader(g_pBlitPS, sizeof(g_pBlitPS));
	pso_blit.SetRenderTargetFormat(g_native_renderer->scene_format, DXGI_FORMAT_UNKNOWN);
	pso_blit.Finalize();


	rs_default.Reset(8, 1);
	rs_default[0].InitAsConstantBuffer(0);
	rs_default[1].InitAsConstants(1,1);
	rs_default[2].InitAsBufferSRV(0);
	rs_default[3].InitAsBufferSRV(1);
	rs_default[4].InitAsBufferSRV(2);
	rs_default[5].InitAsBufferSRV(3);
	rs_default[6].InitAsBufferSRV(4);
	rs_default[7].InitAsDescriptorRange(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 5,255);
	rs_default.InitStaticSampler(0, wakuwaku::GraphicsState::SamplerLinearWarpDesc);
	rs_default.Finalize(L"default signature", D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);

	D3D12_INPUT_ELEMENT_DESC mInputLayout[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
		{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
		{ "TANGENT", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 24, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
		{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 40, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 }
	};

	pso_default = GraphicsPSO(L"default");
	pso_default.SetRootSignature(rs_default);
	pso_default.SetRasterizerState(GraphicsState::RasterizerDefault);
	pso_default.SetBlendState(GraphicsState::BlendTransparent);
	pso_default.SetDepthStencilState(GraphicsState::DepthStateDefault);
	pso_default.SetSampleMask(0xFFFFFFFF);
	pso_default.SetInputLayout(_countof(mInputLayout), mInputLayout);
	pso_default.SetPrimitiveTopologyType(D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE);
	pso_default.SetVertexShader(g_pDefaultVS, sizeof(g_pDefaultVS));
	pso_default.SetPixelShader(g_pDefaultPS, sizeof(g_pDefaultPS));
	pso_default.SetRenderTargetFormat(g_native_renderer->scene_format, DXGI_FORMAT_D32_FLOAT);
	pso_default.Finalize();
}
int wakuwaku::NativeRenderer::AddTexture(int id, void* texture_hanle)
{
	if (texture_map.find(id) != texture_map.end())
	{
		return 0; // exist
	}
	int texture_id = texture_map.size();
	auto& h = texture_map[id];
	
	h.texture_id = texture_id;
	h.texture.Attach(static_cast<ID3D12Resource*>(texture_hanle));
	return 1;
}

int wakuwaku::NativeRenderer::AddMesh(int mesh_id, void* vertex_handle, int num_vectex_elements, void* index_handlem, int num_index_elements, Mesh::SubMesh* subMeshes, int subMesh_count)
{
	if (mesh_map.find(mesh_id) != mesh_map.end())
		return 1; // existed

	Mesh mesh;
	mesh.vectex_buffer.Attach(static_cast<ID3D12Resource*>(vertex_handle), num_vectex_elements);
	mesh.index_buffer.Attach(static_cast<ID3D12Resource*>(index_handlem), num_index_elements);

	mesh.subMeshes.resize(subMesh_count);
	for (size_t i = 0; i < subMesh_count; i++)
	{
		mesh.subMeshes[i] = subMeshes[i];
	}
	mesh_map[mesh_id] = mesh;
	return 0;
}

void wakuwaku::NativeRenderer::Initialize()
{
	OnSize(600, 800, 2, k_format);

	wakuwaku::GraphicsState::Initialize();
	wakuwaku::PSOManager::Initialize();
}

void wakuwaku::OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	// Create graphics API implementation upon initialization
	if (eventType == kUnityGfxDeviceEventInitialize)
	{
		assert(wakuwaku::s_Graphics->GetRenderer() == kUnityGfxRendererD3D12);
		Graphics::Initialize(wakuwaku::s_UnityInterfaces);
	}

	// Cleanup graphics API implementation upon shutdown
	if (eventType == kUnityGfxDeviceEventShutdown)
	{
		Graphics::Shutdown();
	}
}

void wakuwaku::NativeRenderer::Shutdown()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

void wakuwaku::NativeRenderer::Render()
{
	if (instance_cpu_datas.empty())
		return;
	float colors[] = { 1.0,0,0,1 };
	auto& current_frame_buffer = frame_buffers[current_frame_buffer_idx];
	GraphicsContext& context = GraphicsContext::Begin(L"Blit");
	context.SetRootSignature(PSOManager::rs_default);
	context.SetPipelineState(PSOManager::pso_default);
	context.SetViewportAndScissor(0, 0, scene_width, scene_height); 
	context.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_RENDER_TARGET);
	context.SetRenderTarget(current_frame_buffer.GetRTV(),g_native_renderer->scene_depth_buffer.GetDSV());
	context.ClearColor(current_frame_buffer, colors);
	context.ClearDepth(scene_depth_buffer);
	context.SetDynamicConstantBufferView(0,sizeof(wakuwaku::NativeRenderer::CameraData),&g_native_renderer->_camera);
	context.SetDynamicSRV(2, sizeof(Math::Matrix4) * world_matries.size(), world_matries.data());
	context.SetDynamicSRV(3, sizeof(Math::Matrix4) * world_matries.size(), prev_world_matries.data());
	context.SetDynamicSRV(4, sizeof(Math::Matrix4) * world_matries.size(), world_inv_transpose_matries.data());
	context.SetDynamicSRV(5, sizeof(Material) * materials.size(), materials.data());
	context.SetDynamicSRV(6, sizeof(InstanceDataGPU) * instance_gpu_datas.size(), instance_gpu_datas.data());
	
	{
		std::vector<D3D12_CPU_DESCRIPTOR_HANDLE> handles(texture_map.size());
		for (const auto& [ins_id,tex] : texture_map)
		{
			handles[tex.texture_id] = tex.texture.GetSRV();
		}
		context.SetDynamicDescriptors(7, 0, texture_map.size(), handles.data());
	}
	context.SetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
	for (int i = 0; i < instance_cpu_datas.size(); i++)
	{
		auto& ins = instance_cpu_datas[i];

		auto& mesh = mesh_map[ins.mesh_id];
		context.SetVertexBuffer(0, mesh.vectex_buffer.VertexBufferView());
		context.SetIndexBuffer(mesh.index_buffer.IndexBufferView());
		context.SetConstants(1, i);
		context.DrawIndexed(mesh.subMeshes[ins.subMesh_id].index_count, mesh.subMeshes[ins.subMesh_id].index_start, 0);
	}
	context.Finish();
}

void wakuwaku::NativeRenderer::OnSize(int width, int height, int buffer_count, DXGI_FORMAT format)
{
	scene_width = width;
	scene_height = height;

	scene_format = format;

	frame_buffer_count = buffer_count;

	frame_buffers.resize(frame_buffer_count);
	// Rendering
	for (int i = 0; i < frame_buffer_count; i++)
	{
		frame_buffers[i].Create(L"wakuwaku color buffer" + std::to_wstring(i), scene_width, scene_height, 1, scene_format);
	}

	scene_depth_buffer.Create(L"wakuwaku depth buffer", width, height, DXGI_FORMAT_D32_FLOAT);
}


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnSize(int width, int height, int frame_buffer_count,void** frame_buffer_array)
{
	g_native_renderer->OnSize(width, height, frame_buffer_count, wakuwaku::NativeRenderer::k_format);
	
	for (size_t i = 0; i < frame_buffer_count; i++)
	{
		frame_buffer_array[i] = g_native_renderer->frame_buffers[i].GetResource();
	}
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnInitialize()
{
	g_native_renderer.reset(new wakuwaku::NativeRenderer());
	g_native_renderer->Initialize();
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnNativeRendererQuit()
{
	g_native_renderer->Shutdown();
	g_native_renderer = nullptr;
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ApplyCamera(void* camera_data)
{
	g_native_renderer->_camera = *static_cast<wakuwaku::NativeRenderer::CameraData*>(camera_data);
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Render()
{
	g_native_renderer->current_frame_buffer_idx %= g_native_renderer->frame_buffer_count;
	g_native_renderer->Render();
	return g_native_renderer->current_frame_buffer_idx ++ ;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API AddMesh(int mesh_id, void* vertex_handle, int num_vectex_elements, void* index_handlem, int num_index_elements, wakuwaku::NativeRenderer::Mesh::SubMesh * subMeshes, int subMesh_count)
{
	return g_native_renderer->AddMesh(mesh_id,vertex_handle, num_vectex_elements, index_handlem, num_index_elements, subMeshes, subMesh_count);
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API AddTexture(int id,void* texture_handle)
{
	return g_native_renderer->AddTexture(id, texture_handle);
	//ID3D12Resource* 
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API AddMaterial(int id, int albedo_tex_id)
{
	if (g_native_renderer->material_map.find(id) != g_native_renderer->material_map.end())
		return 1; // existed 

	int id_in_vector = g_native_renderer->materials.size();
	g_native_renderer->materials.emplace_back(g_native_renderer->texture_map[albedo_tex_id].texture_id);

	g_native_renderer->material_map[id] = id_in_vector;
	return 0;
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API EndAddRenderItem()
{
	g_native_renderer->instance_cpu_datas.clear();
	g_native_renderer->instance_gpu_datas.clear();
	g_native_renderer->prev_world_matries.clear();
	g_native_renderer->world_inv_transpose_matries.clear();
	g_native_renderer->world_matries.clear();

	for (size_t i = 0; i < g_native_renderer->temp_render_items.size(); i++)
	{
		auto& temp_item = g_native_renderer->temp_render_items[i];
		wakuwaku::NativeRenderer::InstanceDataCPU cpu_data;
		wakuwaku::NativeRenderer::InstanceDataGPU gpu_data;

		const auto& mesh = g_native_renderer->mesh_map.at(temp_item.mesh_id);
		
		for (size_t i = 0; i < mesh.subMeshes.size(); i++)
		{
			wakuwaku::NativeRenderer::InstanceDataCPU cpu_data;
			cpu_data.mesh_id = temp_item.mesh_id;
			cpu_data.subMesh_id = i;

			g_native_renderer->instance_cpu_datas.push_back(cpu_data);

			wakuwaku::NativeRenderer::InstanceDataGPU gpu_data;
			gpu_data.material_id = g_native_renderer->material_map.at(temp_item.material[i]);
			gpu_data.transform_id = g_native_renderer->world_matries.size();

			g_native_renderer->instance_gpu_datas.push_back(gpu_data);
		}
		
		g_native_renderer->world_matries.push_back(temp_item.transform);
		g_native_renderer->prev_world_matries.push_back(temp_item.transform);
		g_native_renderer->world_inv_transpose_matries.push_back(Math::Transpose(Math::Invert(temp_item.transform)));
	}
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API AddRenderItem(int mesh_id,float* transform,int* material_instance_ids,int material_count)
{
	wakuwaku::NativeRenderer::RenderItem temp;
	temp.mesh_id = mesh_id;
	memcpy(&temp.transform, transform, sizeof(Math::Matrix4));
	for (size_t i = 0; i < material_count; i++)
	{
		temp.material.push_back(material_instance_ids[i]);
	}
	g_native_renderer->temp_render_items.push_back(temp);
	return 0;
}

// --------------------------------------------------------------------------
// UnitySetInterfaces
// --------------------------------------------------------------------------
extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces * unityInterfaces)
{
	wakuwaku::s_UnityInterfaces = unityInterfaces;
	wakuwaku::s_Graphics = wakuwaku::s_UnityInterfaces->Get<IUnityGraphics>();

	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	wakuwaku::OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);

	OnInitialize();
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	OnNativeRendererQuit();
}