#include "GraphicsCore.h"
#include "Unity/IUnityGraphicsD3D12.h"
#include "Command/CommandListManager.h"
#include "Command/CommandContext.h"

namespace Graphics
{

	ID3D12Device* g_Device = nullptr;
	CommandListManager g_CommandManager;
	ContextManager g_ContextManager;
	DescriptorAllocator g_DescriptorAllocator[D3D12_DESCRIPTOR_HEAP_TYPE_NUM_TYPES] =
	{
		D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
		D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER,
		D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
		D3D12_DESCRIPTOR_HEAP_TYPE_DSV
	};

	void Initialize(IUnityInterfaces* interfaces)
	{
		auto s_device = interfaces->Get<IUnityGraphicsD3D12v2>();
		g_Device = s_device->GetDevice();

		g_CommandManager.Create(g_Device);
	}

	void Shutdown()
	{

	}


}