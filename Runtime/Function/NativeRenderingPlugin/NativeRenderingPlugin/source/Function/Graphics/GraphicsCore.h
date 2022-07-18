#pragma once
#include "Common.h"
#include "Unity/IUnityInterface.h"
#include "Pipeline/DescriptorHeap.h"

class CommandListManager;
class ContextManager;

namespace Graphics
{
	using namespace Microsoft::WRL;
	

	void Initialize(IUnityInterfaces* interfaces);
	void Shutdown();

	extern ID3D12Device* g_Device;
	extern CommandListManager g_CommandManager;
	extern ContextManager g_ContextManager;
	extern DescriptorAllocator g_DescriptorAllocator[];

	inline D3D12_CPU_DESCRIPTOR_HANDLE AllocateDescriptor(D3D12_DESCRIPTOR_HEAP_TYPE Type, UINT Count = 1)
	{
		return g_DescriptorAllocator[Type].Allocate(Count);
	}
}