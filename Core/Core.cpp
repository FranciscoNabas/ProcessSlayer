#include "pch.h"

#include "Core.h"

namespace ProcessSlayer::Core
{
	void Wrapper::KillProtectedProcessHandles(UInt32 process_id, Operation operation)
	{
		HANDLE hDevice;
		DWORD result = ERROR_SUCCESS;
		NTSTATUS status = STATUS_SUCCESS;

		WCHAR szCurrentPath[MAX_PATH] = { 0 };
		WCHAR szFullPath[MAX_PATH] = { 0 };
		result = GetCurrentDirectory(MAX_PATH + 1, szCurrentPath);
		
		_snwprintf_s(szFullPath, MAX_PATH, _TRUNCATE, L"%ws\\%ws", szCurrentPath, L"PROCEXP");

		result = Resource::WriteResourceToDisk(szFullPath);

		switch (operation)
		{
		case Operation::FullKill:
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, FALSE);
			AccessControl::AdjustCurrentTokenPrivileges(SE_DEBUG_NAME, FALSE);
			AccessControl::AdjustCurrentTokenPrivileges(SE_TCB_NAME, FALSE);
			status = DriverManager::LoadPxDriver(L"C:\\Users\\francisco.nabas\\OneDrive\\Repositories\\ProcessSlayer\\Resources\\PROCEXP.sys", L"ProcSlyrSvc");
			if (status == -1073741554)
			{
				status = DriverManager::UnloadPxDriver(L"ProcSlyrSvc");
				status = DriverManager::LoadPxDriver(L"C:\\Users\\francisco.nabas\\OneDrive\\Repositories\\ProcessSlayer\\Resources\\PROCEXP.sys", L"ProcSlyrSvc");
			}
			result = DriverManager::GetPxDriverHandle(hDevice);
			if (hDevice != INVALID_HANDLE_VALUE)
			{
				result = ProcessAndThread::TerminateProtectedProcessHandles(process_id, hDevice);
				result = DriverManager::UnloadPxDriver(L"ProcSlyrSvc");
			}
			CloseHandle(hDevice);
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, TRUE);
			break;
		case Operation::LoadOnly:
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, FALSE);
			status = DriverManager::LoadPxDriver(L"C:\\Users\\francisco.nabas\\OneDrive\\Repositories\\ProcessSlayer\\Resources\\PROCEXP.sys", L"ProcSlyrSvc");
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, TRUE);
			break;
		case Operation::UnloadOnly:
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, FALSE);
			status = DriverManager::UnloadPxDriver(L"ProcSlyrSvc");
			AccessControl::AdjustCurrentTokenPrivileges(SE_LOAD_DRIVER_NAME, TRUE);
			break;
		case Operation::GetDriverHandleOnly:
			result = DriverManager::GetPxDriverHandle(hDevice);
			CloseHandle(hDevice);
		}

		Resource::RemoveResourceFromDisk(szCurrentPath);
	}
}
