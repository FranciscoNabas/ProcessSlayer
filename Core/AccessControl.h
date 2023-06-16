#pragma once
#pragma unmanaged

#include "pch.h"

namespace ProcessSlayer::Core
{
	extern "C" public class AccessControl
	{
	public:
		static DWORD AdjustCurrentTokenPrivileges(const LPCWSTR& lpszPrivilegeName, BOOL remove);
	};
}