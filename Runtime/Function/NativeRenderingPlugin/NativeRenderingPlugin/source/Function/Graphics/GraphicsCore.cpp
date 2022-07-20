#include "GraphicsCore.h"
#include "Unity/IUnityGraphicsD3D12.h"
#include "Command/CommandListManager.h"
#include "Command/CommandContext.h"
#include <dxgidebug.h>
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
		auto s_device = interfaces->Get<IUnityGraphicsD3D12v5>();
		g_Device = s_device->GetDevice();
		auto  hr1 = g_Device->GetDeviceRemovedReason(); 
		D3D12_FEATURE_DATA_D3D12_OPTIONS5 featureSupportData = {};
		ASSERT_SUCCEEDED((g_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS5, &featureSupportData, sizeof(featureSupportData)))
			&& featureSupportData.RaytracingTier != D3D12_RAYTRACING_TIER_NOT_SUPPORTED);
// 		bool use_debug_layer = false	;
// 		if (use_debug_layer)
// 		{
// 			Microsoft::WRL::ComPtr<ID3D12Debug> debugInterface;
// 			if (SUCCEEDED(D3D12GetDebugInterface(MY_IID_PPV_ARGS(&debugInterface))))
// 			{
// 				debugInterface->EnableDebugLayer();
// 				auto  hr34= g_Device->GetDeviceRemovedReason();
// 				uint32_t useGPUBasedValidation = 0;
// 				if (useGPUBasedValidation)
// 				{
// 					Microsoft::WRL::ComPtr<ID3D12Debug1> debugInterface1;
// 					if (SUCCEEDED((debugInterface->QueryInterface(MY_IID_PPV_ARGS(&debugInterface1)))))
// 					{
// 						debugInterface1->SetEnableGPUBasedValidation(true);
// 					}
// 				}
// 			}
// 			else
// 			{
// 				Utility::Print("WARNING:  Unable to enable D3D12 debug validation layer\n");
// 			}
// 			auto  hr2 = g_Device->GetDeviceRemovedReason();
// 			ComPtr<IDXGIInfoQueue> dxgiInfoQueue;
// 			if (SUCCEEDED(DXGIGetDebugInterface1(0, IID_PPV_ARGS(dxgiInfoQueue.GetAddressOf()))))
// 			{
// 				auto dxgiFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
// 
// 				dxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR, true);
// 				dxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION, true);
// 
// 				DXGI_INFO_QUEUE_MESSAGE_ID hide[] =
// 				{
// 					80 /* IDXGISwapChain::GetContainingOutput: The swapchain's adapter does not control the output on which the swapchain's window resides. */,
// 				};
// 				DXGI_INFO_QUEUE_FILTER filter = {};
// 				filter.DenyList.NumIDs = _countof(hide);
// 				filter.DenyList.pIDList = hide;
// 				dxgiInfoQueue->AddStorageFilterEntries(DXGI_DEBUG_DXGI, &filter);
// 			}
// 			auto  hr3 = g_Device->GetDeviceRemovedReason();
// 		}

// 		ID3D12InfoQueue* pInfoQueue = nullptr;
// 		if (SUCCEEDED(g_Device->QueryInterface(MY_IID_PPV_ARGS(&pInfoQueue))))
// 		{
// 			// Suppress whole categories of messages
// 			//D3D12_MESSAGE_CATEGORY Categories[] = {};
// 
// 			// Suppress messages based on their severity level
// 			D3D12_MESSAGE_SEVERITY Severities[] =
// 			{
// 				D3D12_MESSAGE_SEVERITY_INFO
// 			};
// 
// 			// Suppress individual messages by their ID
// 			D3D12_MESSAGE_ID DenyIds[] =
// 			{
// 				// This occurs when there are uninitialized descriptors in a descriptor table, even when a
// 				// shader does not access the missing descriptors.  I find this is common when switching
// 				// shader permutations and not wanting to change much code to reorder resources.
// 				D3D12_MESSAGE_ID_INVALID_DESCRIPTOR_HANDLE,
// 
// 				// Triggered when a shader does not export all color components of a render target, such as
// 				// when only writing RGB to an R10G10B10A2 buffer, ignoring alpha.
// 				D3D12_MESSAGE_ID_CREATEGRAPHICSPIPELINESTATE_PS_OUTPUT_RT_OUTPUT_MISMATCH,
// 
// 				// This occurs when a descriptor table is unbound even when a shader does not access the missing
// 				// descriptors.  This is common with a root signature shared between disparate shaders that
// 				// don't all need the same types of resources.
// 				D3D12_MESSAGE_ID_COMMAND_LIST_DESCRIPTOR_TABLE_NOT_SET,
// 
// 				// RESOURCE_BARRIER_DUPLICATE_SUBRESOURCE_TRANSITIONS
// 				(D3D12_MESSAGE_ID)1008,
// 			};
// 
// 			D3D12_INFO_QUEUE_FILTER NewFilter = {};
// 			//NewFilter.DenyList.NumCategories = _countof(Categories);
// 			//NewFilter.DenyList.pCategoryList = Categories;
// 			NewFilter.DenyList.NumSeverities = _countof(Severities);
// 			NewFilter.DenyList.pSeverityList = Severities;
// 			NewFilter.DenyList.NumIDs = _countof(DenyIds);
// 			NewFilter.DenyList.pIDList = DenyIds;
// 
// 			pInfoQueue->PushStorageFilter(&NewFilter);
// 			pInfoQueue->Release();
// 		}

		g_CommandManager.Create(g_Device,s_device->GetCommandQueue());
	}

	void Shutdown()
	{

	}


}