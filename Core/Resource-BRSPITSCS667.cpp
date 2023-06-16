#include "pch.h"

#include "resource.h"

namespace ProcessSlayer::Core
{
	DWORD Resource::WriteResourceToDisk(const LPCWSTR& lpszPath)
	{
		DWORD result = ERROR_SUCCESS;
		DWORD dwBytesWritten;
		
		HRSRC hResource = FindResource(NULL, MAKEINTRESOURCE(PS_PROCEXPBIN), RT_RCDATA);
		WIN32RETURNLASTERRORIFNULL(hResource);

		HGLOBAL hgLoadedRes = LoadResource(nullptr, hResource);
		WIN32RETURNLASTERRORIFNULL(hgLoadedRes);

		LPVOID lpLock = LockResource(hgLoadedRes);
		WIN32RETURNLASTERRORIFNULL(lpLock);

		DWORD dwResSz = SizeofResource(NULL, hResource);
		WIN32RETURNLASTERRORIFNULL(dwResSz);

		HANDLE hFile = CreateFile(lpszPath, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
		if (hFile == INVALID_HANDLE_VALUE)
			return GetLastError();

		if (!WriteFile(hFile, lpLock, dwResSz, &dwBytesWritten, nullptr))
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