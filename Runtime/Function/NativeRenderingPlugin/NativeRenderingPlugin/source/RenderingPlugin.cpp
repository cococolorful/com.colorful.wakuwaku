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


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API 
	RegisterLog(
		wakuwaku::LogManager::LogFunc log_info, 
		wakuwaku::LogManager::LogFunc log_warning, 
		wakuwaku::LogManager::LogFunc log_error)
{
	wakuwaku::LogManager::Instance().Initialize(log_info, log_warning, log_error);
}

namespace wakuwaku::NativeRenderer
{
	// Unity related
	IUnityInterfaces* s_UnityInterfaces = NULL;
	IUnityGraphics* s_Graphics = NULL;

	void Initialize(IUnityInterfaces* unityInterfaces);
	void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
	void Shutdown();

	// Rendering
	size_t scene_width = 600;
	size_t scene_height = 800;
	DXGI_FORMAT scene_format = DXGI_FORMAT_R32G32B32A32_FLOAT;

	int frame_buffer_count = 3;
	std::vector< ColorBuffer> frame_buffers(frame_buffer_count);
	
	void OnSize(int width, int height, int frame_buffer_count,DXGI_FORMAT format);
	
	void Render();
};

void wakuwaku::NativeRenderer::Initialize(IUnityInterfaces* unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
	s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();

	s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);

	
}
void wakuwaku::NativeRenderer::OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	// Create graphics API implementation upon initialization
	if (eventType == kUnityGfxDeviceEventInitialize)
	{
		assert(s_Graphics->GetRenderer() == kUnityGfxRendererD3D12);
		Graphics::Initialize(s_UnityInterfaces);
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
	auto& ctx = GraphicsContext::Begin(L"modify texture");
	static Color c;
	static int idx = 0;
	idx %= 3;
	static int r = 0;
	c[0] = (r + 1) / 255;
	r %= 255;

	auto& current_frame_buffer = frame_buffers[idx++];

	ctx.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_RENDER_TARGET);
	ctx.ClearColor(frame_buffers[idx], c.GetPtr());
	ctx.TransitionResource(current_frame_buffer, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
	ctx.Finish();
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

// --------------------------------------------------------------------------
// UnitySetInterfaces
extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	wakuwaku::NativeRenderer::Initialize(unityInterfaces);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	wakuwaku::NativeRenderer::Shutdown();
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnSize(int width, int height, int frame_buffer_count,void** frame_buffer_array)
{
	wakuwaku::NativeRenderer::OnSize(width, height, frame_buffer_count,DXGI_FORMAT_R32G32B32A32_FLOAT);
	
	for (size_t i = 0; i < frame_buffer_count; i++)
	{
		frame_buffer_array[i] = wakuwaku::NativeRenderer::frame_buffers[i].GetResource();
	}
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Render()
{
	wakuwaku::NativeRenderer::Render();
}
