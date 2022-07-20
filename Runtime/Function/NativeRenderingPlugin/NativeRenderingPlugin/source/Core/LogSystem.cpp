#include "LogSystem.h"
#include <windows.h>

void wakuwaku::LogManager::Info(const std::string& info)
{
#if DEBUG
	assert(m_log_info_func != nullptr);
#endif
	auto str = "state[info]: " + info;
	OutputDebugStringA(str.data());
	//m_log_info_func(str.data(),str.size());
}

void wakuwaku::LogManager::Warning(const std::string& info)
{
#if DEBUG
	assert(m_log_warning_func != nullptr);
#endif
	auto str = "state[warning]: " + info;
	OutputDebugStringA(str.data());
	//m_log_warning_func(str.data(), str.size());
}

void wakuwaku::LogManager::Error(const std::string& info)
{
#if DEBUG
	assert(m_log_error_func != nullptr);
#endif
	auto str = "state[error]: " + info;

	OutputDebugStringA(str.data());
	//m_log_error_func(str.data(), str.size());
}
