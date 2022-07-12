#pragma once
#include <string>
#include "../PlatformBase.h"
#include "../Unity/IUnityInterface.h"


namespace wakuwaku::Log
{
	typedef void (UNITY_INTERFACE_API* LogFunc)(char* info);

	void Initialize(LogFunc log);

	void Info(const std::string& info);
}




extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API RegisterLog(LogFunc log)
{
	log_func = log;
}