// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

#ifndef UNICODE
#define UNICODE
#endif //UNICODE

#pragma comment(lib, "Advapi32")

#include <Windows.h>
#include <subauth.h>
#include <wininet.h>
#include <memory>
#include <vector>

#define WIN32FAILED(dwResult) (DWORD)result != ERROR_SUCCESS
#define WIN32RETURNIFFAILED(dwResult) if ((DWORD)dwResult != ERROR_SUCCESS) { return (DWORD)dwResult; }
#define WIN32RETURNLASTERRORIFNULL(obj) if (obj == NULL) { return GetLastError(); }

#define STATUS_BUFFER_TOO_SMALL 0xC0000023L
#define STATUS_BUFFER_OVERFLOW 0x80000005
#define STATUS_INFO_LENGTH_MISMATCH 0xc0000004

#define STATUS_NO_BYTES_RETURNED_FROM_DEVICE 0x0000029A

#define IOCTL_CLOSE_HANDLE 2201288708
#define IOCTL_OPEN_PROTECTED_PROCESS_HANDLE 2201288764
#define IOCTL_GET_HANDLE_NAME 2201288776
#define IOCTL_GET_HANDLE_TYPE 2201288780

#endif //PCH_H
