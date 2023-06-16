#include "pch.h"

#include "AccessControl.h"

namespace ProcessSlayer::Core
{
	DWORD AccessControl::AdjustCurrentTokenPrivileges(const LPCWSTR& lpszPrivilegeName, BOOL remove)
	{
		DWORD result = ERROR_SUCCESS;
		HANDLE hToken;

		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, &hToken))
			return GetLastError();

		std::unique_ptr<TOKEN_PRIVILEGES> spTokenPrivileges = std::make_unique<TOKEN_PRIVILEGES>();
		spTokenPrivileges->PrivilegeCount = 1;
		if (!LookupPrivilegeValue(NULL, lpszPrivilegeName, &spTokenPrivileges->Privileges->Luid))
		{
			CloseHandle(hToken);
			return GetLastError();
		}
		if (remove)
			spTokenPrivileges->Privileges->Attributes = SE_PRIVILEGE_REMOVED;
		else
			spTokenPrivileges->Privileges->Attributes = SE_PRIVILEGE_ENABLED;

		if (!AdjustTokenPrivileges(hToken, FALSE, spTokenPrivileges.get(), 0, NULL, NULL))
			result = GetLastError();
		
		CloseHandle(hToken);
		return result;
	}
}