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


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API 
	RegisterLog(
		wakuwaku::LogManager::LogFunc log_info, 
		wakuwaku::LogManager::LogFunc log_warning, 
		wakuwaku::LogManager::LogFunc log_error)
{
	wakuwaku::LogManager::Instance().Initialize(log_info, log_warning, log_error);
}

namespace wakuwaku
{
	void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
	// Unity related
	IUnityInterfaces* s_UnityInterfaces = NULL;
	IUnityGraphics* s_Graphics = NULL;

	class NativeRenderer
	{
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
			std::vector<int> material;
		};

		int AddMesh(int mesh_id, void* vertex_handle, int num_vectex_elements, void* index_handlem, int num_index_elements, Mesh::SubMesh* subMeshes, int subMesh_count);

		// texture
		std::unordered_map<int, Texture> texture_map;
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
			Math::Matrix4 prev_view_proj_no_jittermatrix;
			Math::Matrix4 proj_no_jitter_matrix;
		};
		CameraData _camera;
	};
};

std::unique_ptr< wakuwaku::NativeRenderer> g_native_renderer;

namespace wakuwaku::GraphicsState
{
	SamplerDesc SamplerLinearClampDesc;

	
	D3D12_RASTERIZER_DESC RasterizerDefault; // counter-clockwise
	D3D12_RASTERIZER_DESC RasterizerDefaultCw; // clockwise

	D3D12_BLEND_DESC BlendNoColorWrite;
	D3D12_BLEND_DESC BlendDisable;

	D3D12_DEPTH_STENCIL_DESC DepthStateDisabled;

	void Initialize();
}	
void wakuwaku::GraphicsState::Initialize()
{
	SamplerLinearClampDesc.Filter = D3D12_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR;
	SamplerLinearClampDesc.SetTextureAddressMode(D3D12_TEXTURE_ADDRESS_MODE_CLAMP);
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
	D3D12_BLEND_DESC alphaBlend = {};
	alphaBlend.IndependentBlendEnable = FALSE;
	alphaBlend.RenderTarget[0].BlendEnable = FALSE;
	alphaBlend.RenderTarget[0].SrcBlend = D3D12_BLEND_SRC_ALPHA;
	alphaBlend.RenderTarget[0].DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
	alphaBlend.RenderTarget[0].BlendOp = D3D12_BLEND_OP_ADD;
	alphaBlend.RenderTarget[0].SrcBlendAlpha = D3D12_BLEND_ONE;
	alphaBlend.RenderTarget[0].DestBlendAlpha = D3D12_BLEND_INV_SRC_ALPHA;
	alphaBlend.RenderTarget[0].BlendOpAlpha = D3D12_BLEND_OP_ADD;
	alphaBlend.RenderTarget[0].RenderTargetWriteMask = 0;
	BlendNoColorWrite = alphaBlend;

	alphaBlend.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;
	BlendDisable = alphaBlend;

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
}
namespace wakuwaku::PSOManager
{
	RootSignature rs_present;
	GraphicsPSO pso_blit;

	void Initialize();
}
void wakuwaku::PSOManager::Initialize()
{
	rs_present.Reset(1, 1);
	CD3DX12_RASTERIZER_DESC s;
//  rs_present[0].InitAsDescriptorRange(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 0, 2);
// 	rs_present[1].InitAsConstants(0, 6, D3D12_SHADER_VISIBILITY_ALL);
// 	rs_present[2].InitAsConstants(0, 6, D3D12_SHADER_VISIBILITY_ALL);
	rs_present[0].InitAsDescriptorRange(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 0, 1);
	rs_present.InitStaticSampler(0, GraphicsState::SamplerLinearClampDesc);
	rs_present.Finalize(L"present");

	pso_blit.SetRootSignature(rs_present);
	pso_blit.SetRasterizerState(GraphicsState::RasterizerDefault);
	pso_blit.SetBlendState(GraphicsState::BlendDisable);
	//pso_blit.SetDepthStencilState(CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT));

	//pso_blit.SetRasterizerState(GraphicsState::RasterizerDefault);
	//pso_blit.SetBlendState(GraphicsState::BlendDisable);
	pso_blit.SetDepthStencilState(GraphicsState::DepthStateDisabled);
	pso_blit.SetSampleMask(0xFFFFFFFF);
	pso_blit.SetInputLayout(0, nullptr);
	pso_blit.SetPrimitiveTopologyType(D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE);
	pso_blit.SetVertexShader(g_pScreenQuadVS, sizeof(g_pScreenQuadVS));
	pso_blit.SetPixelShader(g_pBlitPS, sizeof(g_pBlitPS));
	pso_blit.SetRenderTargetFormat(g_native_renderer->scene_format, DXGI_FORMAT_UNKNOWN);
	pso_blit.Finalize();
}
int wakuwaku::NativeRenderer::AddTexture(int id, void* texture_hanle)
{
	if (texture_map.find(id) != texture_map.end())
	{
		return 0; // exist
	}
	
	texture_map[id].Attach(static_cast<ID3D12Resource*>(texture_hanle));

	return 1;
}

int wakuwaku::NativeRenderer::AddMesh(int mesh_id, void* vertex_handle, int num_vectex_elements, void* index_handlem, int num_index_elements, Mesh::SubMesh* subMeshes, int subMesh_count)
{
	if (mesh_map.find(mesh_id) != mesh_map.end())
		return 1; // existed

	Mesh mesh;
	mesh.vectex_buffer.Attach(static_cast<ID3D12Resource*>(vertex_handle), num_vectex_elements);
	mesh.index_buffer.Attach(static_cast<ID3D12Resource*>(vertex_handle), num_vectex_elements);

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

	static Color c;
	static int r = 0;
	c[0] = (r ++) / 255.0f;
	r %= 255;

	auto& current_frame_buffer = frame_buffers[current_frame_buffer_idx];

	// blit
	if(texture_map.begin() != texture_map.end())
	{
		float colors[] = { 1.0,0,0,1 };
		auto& tex = *texture_map.begin();
		GraphicsContext& context = GraphicsContext::Begin(L"Blit");
		context.SetRootSignature(PSOManager::rs_present);
		context.SetPipelineState(PSOManager::pso_blit);

		context.TransitionResource(tex.second, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
		context.SetDynamicDescriptor(0, 0, tex.second.GetSRV());
		context.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_RENDER_TARGET);
		context.SetRenderTarget(current_frame_buffer.GetRTV());
		context.ClearColor(current_frame_buffer, colors);
		context.SetViewportAndScissor(0, 0, scene_width, scene_height);
		context.Draw(3);

		context.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
		context.Finish();
	}
	else
	{
		auto& ctx = GraphicsContext::Begin(L"modify texture");

		ctx.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_RENDER_TARGET);
		ctx.ClearColor(current_frame_buffer, c.GetPtr());

		ctx.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
		ctx.Finish();
	}

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
		frame_buffers[i].Create(L"test" + std::to_wstring(i), scene_width, scene_height, 1, scene_format);
	}
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
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API AddMaterial(int id,void* texture_handle)
{
	return g_native_renderer->AddTexture(id, texture_handle);
	//ID3D12Resource* 
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