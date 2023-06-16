#include "pch.h"

#include "ProcessAndThread.h"

namespace ProcessSlayer::Core
{
	NTSTATUS ProcessAndThread::TerminateProtectedProcessHandles(ULONG ulProcessId, HANDLE& hDevice)
	{
		NTSTATUS result = STATUS_SUCCESS;
		ULONG ulBufferSize = 1 << 12;
		HANDLE hProcess;

		HMODULE hNtdll = LoadLibrary(L"ntdll.dll");
		if (hNtdll == NULL)
			return (NTSTATUS)GetLastError();

		_NtQuerySystemInformation NtQuerySystemInformation = (_NtQuerySystemInformation)GetProcAddress(hNtdll, "NtQuerySystemInformation");

		result = ProcessAndThread::GetProtectedProcessHandle(ulProcessId, hProcess, hDevice);
		if (result != STATUS_SUCCESS)
		{
			FreeLibrary(hNtdll);
			return result;
		}

		std::unique_ptr<BYTE[]> buffer;
		do
		{
			buffer = std::make_unique<BYTE[]>(ulBufferSize);
			result = NtQuerySystemInformation(SystemHandleInformation, buffer.get(), ulBufferSize, &ulBufferSize);

			if (result == STATUS_SUCCESS)
				break;

			if (result != STATUS_INFO_LENGTH_MISMATCH &&
				result != STATUS_BUFFER_OVERFLOW &&
				result != STATUS_BUFFER_TOO_SMALL)
			{
				FreeLibrary(hNtdll);
				return result;
			}

		} while (result == STATUS_BUFFER_TOO_SMALL || result == STATUS_BUFFER_OVERFLOW || result == STATUS_INFO_LENGTH_MISMATCH);

		PSYSTEM_HANDLE_INFORMATION pshiHandleInfo = reinterpret_cast<PSYSTEM_HANDLE_INFORMATION>(buffer.get());
		
		size_t szEntry = sizeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO);
		PSYSTEM_HANDLE_TABLE_ENTRY_INFO pvArrayPtr = pshiHandleInfo->Handles;
		for (DWORD i = 0; i < pshiHandleInfo->NumberOfHandles; i++)
		{
			auto current = std::make_unique<SYSTEM_HANDLE_TABLE_ENTRY_INFO>();
			CopyMemory(current.get(), pvArrayPtr, szEntry);
			if (current->UniqueProcessId == ulProcessId)
			{
				// Check if the process is already killed every 15 closed handles otherwise we'll keep trying to close handles that are already closed.
				if (i % 15 == 0)
				{
					DWORD dwProcExitCode;
					GetExitCodeProcess(hProcess, &dwProcExitCode);
					if (dwProcExitCode != STILL_ACTIVE)
						return STATUS_SUCCESS;
				}

				auto ppMessenger = std::make_unique<PX_IO_CONTROL>();
				ppMessenger->ulPid = ulProcessId;
				ppMessenger->ulSize = 0;
				ppMessenger->ulHandle = current->HandleValue;
				ppMessenger->lpObjectAddress = current->Object;

				// TODO: Better handle errors in case only one handle fails.
				if (!DeviceIoControl(hDevice, IOCTL_CLOSE_HANDLE, (LPVOID)ppMessenger.get(), sizeof(PX_IO_CONTROL), NULL, 0, NULL, NULL))
					return (NTSTATUS)GetLastError();
			}
			pvArrayPtr++;
		}

		return result;
	}

	NTSTATUS ProcessAndThread::GetProtectedProcessHandle(ULONG ulProcessId, HANDLE& hProcess, HANDLE& hDevice)
	{
		NTSTATUS result = STATUS_SUCCESS;
		DWORD dwBytesReturned;

		if (!DeviceIoControl(hDevice, IOCTL_OPEN_PROTECTED_PROCESS_HANDLE, (LPVOID)&ulProcessId, sizeof(ULONG), &hProcess, sizeof(HANDLE), &dwBytesReturned, NULL))
			return (NTSTATUS)GetLastError();

		if (dwBytesReturned == 0)
			return STATUS_NO_BYTES_RETURNED_FROM_DEVICE;

		return result;
	}
}