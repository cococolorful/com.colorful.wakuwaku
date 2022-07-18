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




// --------------------------------------------------------------------------
// UnitySetInterfaces

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);

static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
	s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
	s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

	
	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}



// --------------------------------------------------------------------------
// GraphicsDeviceEvent


static UnityGfxRenderer s_DeviceType = kUnityGfxRendererNull;


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	// Create graphics API implementation upon initialization
	if (eventType == kUnityGfxDeviceEventInitialize)
	{
		s_DeviceType = s_Graphics->GetRenderer();
		assert(s_DeviceType == kUnityGfxRendererD3D12);
		Graphics::Initialize(s_UnityInterfaces);
	}


	// Cleanup graphics API implementation upon shutdown
	if (eventType == kUnityGfxDeviceEventShutdown)
	{
		Graphics::Shutdown();
		s_DeviceType = kUnityGfxRendererNull;
	}
}
ColorBuffer color;
static bool first = true;
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ModifyTexture(void* texture_handle, float* color4)
{
	if (texture_handle == nullptr)
	{
		wakuwaku::LogManager::Instance().Info("the texture is null");
			return;
	}
	if (first)
	{
		color.Create(L"test", 600, 800, 1, DXGI_FORMAT_R16G16B16A16_FLOAT);
		first = false;
	}
	auto& ctx = GraphicsContext::Begin(L"modify texture");

	ctx.ClearColor(color, color4);

	//GpuResource dst{ static_cast<ID3D12Resource*>(texture_handle),D3D12_RESOURCE_STATE_COPY_DEST };
	//
	//ctx.ClearUAV (dst, 0, 0, 0, color);
	ctx.Finish(true);
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Render(void* back_buffer)
{
	
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DrawMesh(void* vertex_buffer_handle, void* index_buffer_handle, float* MVP, void* rt)
{
	
}

// --------------------------------------------------------------------------
// GetRenderEventFunc, an example function we export which is used to get a rendering event callback function.

void OnRenderEvent(int event_id)
{

}
extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc()
{
	return OnRenderEvent;
}

