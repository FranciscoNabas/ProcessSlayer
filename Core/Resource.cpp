#include "pch.h"

#include "Core.h"
#include "resource.h"

namespace ProcessSlayer::Core
{
	DWORD Resource::WriteResourceToDisk(const LPCWSTR& lpszPath)
	{
		DWORD result = ERROR_SUCCESS;
		DWORD dwBytesWritten;
		
		HMODULE hCurrentModule = GetModuleHandleW(L"Core.dll");
		HRSRC hResource = FindResource(hCurrentModule, MAKEINTRESOURCE(1), RT_RCDATA);
		WIN32RETURNLASTERRORIFNULL(hResource);

		HGLOBAL hgLoadedRes = LoadResource(hCurrentModule, hResource);
		WIN32RETURNLASTERRORIFNULL(hgLoadedRes);

		LPVOID lpLock = LockResource(hgLoadedRes);
		WIN32RETURNLASTERRORIFNULL(lpLock);

		DWORD dwResSz = SizeofResource(hCurrentModule, hResource);
		WIN32RETURNLASTERRORIFNULL(dwResSz);

		HANDLE hFile = CreateFile(lpszPath, GENERIC_WRITE, 0,NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hFile == INVALID_HANDLE_VALUE)
			return GetLastError();

		if (!WriteFile(hFile, lpLock, dwResSz, &dwBytesWritten, NULL))
			result = GetLastError();

		CloseHandle(hFile);
		return result;
	}

	DWORD Resource::RemoveResourceFromDisk(const LPCWSTR& lpszPath)
	{
		if (!DeleteFile(lpszPath))
			return GetLastError();

		return ERROR_SUCCESS;
	}
}