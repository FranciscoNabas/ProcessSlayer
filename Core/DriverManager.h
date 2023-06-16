#pragma once
#pragma unmanaged

#include "pch.h"

namespace ProcessSlayer::Core
{
	typedef void (NTAPI* _RtlInitUnicodeString)(
		__out PUNICODE_STRING DestinationString,
		__in __drv_aliasesMem PCWSTR SourceString
	);

	typedef NTSTATUS(NTAPI* _NtLoadDriver)(
		__in PUNICODE_STRING DriverServiceName
	);

	typedef NTSTATUS(NTAPI* _NtUnloadDriver)(
		__in PUNICODE_STRING DriverServiceName
	);

	typedef struct _PX_IO_CONTROL
	{
		ULONGLONG ulPid;
		PVOID lpObjectAddress;
		ULONGLONG ulSize;
		ULONGLONG ulHandle;
	} PX_IO_CONTROL, *PPX_IO_CONTROL;

	extern "C" public class DriverManager
	{
	public:
		static NTSTATUS LoadPxDriver(const LPCWSTR& lpszDriverPath, const LPCWSTR& lpszServiceName);
		static NTSTATUS UnloadPxDriver(const LPCWSTR& lpszServiceName);
		static DWORD GetPxDriverHandle(HANDLE& hDevice);
		static DWORD SetRegistryValues(const LPCWSTR& lpszDriverPath, const LPCWSTR& lpszServiceName);
		static DWORD RemoveServiceRegistryKey(const LPCWSTR& lpszServiceName);
	};
}