#pragma once
#include <string>


namespace wakuwaku
{
	class LogManager
	{
	public:

		using LogFunc = void(*)(char* info,int size);
		static LogManager& Instance()
		{
			static LogManager instance;
			return instance;
		}
		void Initialize(LogFunc log_info,LogFunc log_warning = nullptr,LogFunc log_error = nullptr)
		{
			m_log_info_func = log_info;
			m_log_warning_func = log_warning;
			m_log_error_func = log_error;
		}

		void Info(const std::string& info);
		void Warning(const std::string& info);
		void Error(const std::string& info);
	private:

		LogManager() = default;

		LogFunc m_log_info_func;
		LogFunc m_log_warning_func;
		LogFunc m_log_error_func;
	};
	
}

