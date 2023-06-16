#pragma once
#pragma unmanaged

#include "AccessControl.h"
#include "DriverManager.h"
#include "ProcessAndThread.h"
#include "Resource.h"

#pragma managed

using namespace System;

namespace ProcessSlayer::Core {
	public enum class Operation {
		FullKill,
		LoadOnly,
		UnloadOnly,
		GetDriverHandleOnly,
	};
	public ref class Wrapper
	{
	public:
		void KillProtectedProcessHandles(UInt32 process_id, Operation operation);
	};

	extern "C" public class Resource
	{
	public:
		static DWORD WriteResourceToDisk(const LPCWSTR& lpszPath);
		static DWORD RemoveResourceFromDisk(const LPCWSTR& lpszPath);
	};
}
