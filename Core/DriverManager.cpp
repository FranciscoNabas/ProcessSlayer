#include "pch.h"

#include "DriverManager.h"
#include "AccessControl.h"
#include "Resource.h"

namespace ProcessSlayer::Core
{
	NTSTATUS DriverManager::LoadPxDriver(const LPCWSTR& lpszDriverPath, const LPCWSTR& lpszServiceName)
	{
		UNICODE_STRING uszDriverName;
		NTSTATUS result = ERROR_SUCCESS;
		WCHAR szNtDriverPath[MAX_PATH] = { 0 };

		HMODULE hNtdll = LoadLibrary(L"ntdll.dll");
		if (hNtdll == NULL)
			return (NTSTATUS)GetLastError();

		__try
		{
			_RtlInitUnicodeString RtlInitUnicodeString = (_RtlInitUnicodeString)GetProcAddress(hNtdll, "RtlInitUnicodeString");
			_NtLoadDriver NtLoadDriver = (_NtLoadDriver)GetProcAddress(hNtdll, "NtLoadDriver");

			DriverManager::SetRegistryValues(lpszDriverPath, lpszServiceName);

			_snwprintf_s(szNtDriverPath, MAX_PATH, _TRUNCATE, L"\\Registry\\Machine\\System\\CurrentControlSet\\Services\\%ws", lpszServiceName);
			RtlInitUnicodeString(&uszDriverName, szNtDriverPath);

			result = NtLoadDriver(&uszDriverName);
		}
		__finally
		{
			FreeLibrary(hNtdll);
		}

		return result;
	}

	NTSTATUS DriverManager::UnloadPxDriver(const LPCWSTR& lpszServiceName)
	{
		UNICODE_STRING uszDriverName;
		NTSTATUS result = ERROR_SUCCESS;
		WCHAR szNtDriverPath[MAX_PATH] = { 0 };

		HMODULE hNtdll = LoadLibrary(L"ntdll.dll");
		if (hNtdll == NULL)
			return (NTSTATUS)GetLastError();

		__try
		{
			_RtlInitUnicodeString RtlInitUnicodeString = (_RtlInitUnicodeString)GetProcAddress(hNtdll, "RtlInitUnicodeString");
			_NtUnloadDriver NtUnloadDriver = (_NtUnloadDriver)GetProcAddress(hNtdll, "NtUnloadDriver");

			_snwprintf_s(szNtDriverPath, MAX_PATH, _TRUNCATE, L"\\Registry\\Machine\\System\\CurrentControlSet\\Services\\%ws", lpszServiceName);
			RtlInitUnicodeString(&uszDriverName, szNtDriverPath);

			result = NtUnloadDriver(&uszDriverName);
		}
		__finally
		{
			FreeLibrary(hNtdll);
			DriverManager::RemoveServiceRegistryKey(lpszServiceName);
		}

		return result;
	}

	DWORD DriverManager::GetPxDriverHandle(HANDLE& hDevice)
	{
		WCHAR szDevicePath[MAX_PATH] = {0};

		hDevice = CreateFileA("\\\\.\\PROCEXP152", GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
		if (hDevice == INVALID_HANDLE_VALUE)
			return GetLastError();

		return ERROR_SUCCESS;
	}

	DWORD DriverManager::SetRegistryValues(const LPCWSTR& lpszDriverPath, const LPCWSTR& lpszServiceName)
	{
		HKEY hKey;
		DWORD dwData = 0;
		LSTATUS result = ERROR_SUCCESS;
		WCHAR szRegPath[MAX_PATH] = { 0 };
		WCHAR szFullDriverPath[MAX_PATH] = { 0 };

		_snwprintf_s(szRegPath, MAX_PATH, _TRUNCATE, L"SYSTEM\\CurrentControlSet\\Services\\%ws", lpszServiceName);
		_snwprintf_s(szFullDriverPath, MAX_PATH, _TRUNCATE, L"\\??\\%ws", lpszDriverPath);
		DWORD dwDriverPathSize = wcslen(szFullDriverPath) + 1;

		result = RegCreateKeyEx(HKEY_LOCAL_MACHINE, szRegPath, 0, NULL, 0, KEY_ALL_ACCESS, NULL, &hKey, NULL);
		WIN32RETURNIFFAILED(result);

		for (LPCWSTR lpszName : { L"Type", L"ErrorControl", L"Start" }) {
			result = RegSetValueEx(hKey, lpszName, 0, REG_DWORD, (BYTE*)&dwData, sizeof(dwData));
			WIN32RETURNIFFAILED(result);
		}

		result = RegSetValueEx(hKey, L"ImagePath", 0, REG_SZ, (BYTE*)&szFullDriverPath, sizeof(WCHAR) * dwDriverPathSize);
		WIN32RETURNIFFAILED(result);

		return (DWORD)result;
	}

	DWORD DriverManager::RemoveServiceRegistryKey(const LPCWSTR& lpszServiceName)
	{
		LSTATUS result = ERROR_SUCCESS;
		WCHAR szRegPath[MAX_PATH] = { 0 };
		
		_snwprintf_s(szRegPath, MAX_PATH, _TRUNCATE, L"SYSTEM\\CurrentControlSet\\Services\\%ws", lpszServiceName);
	
		return (DWORD)RegDeleteKeyEx(HKEY_LOCAL_MACHINE, szRegPath, KEY_WOW64_64KEY, 0);
	}
}