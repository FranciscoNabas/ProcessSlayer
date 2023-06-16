#pragma once
#pragma unmanaged

#include "pch.h"

#include "DriverManager.h"

namespace ProcessSlayer::Core
{
    typedef enum _SYSTEM_INFORMATION_CLASS
    {
        SystemBasicInformation = 0,
        SystemProcessorInformation = 1,
        SystemPerformanceInformation = 2,
        SystemTimeOfDayInformation = 3,
        SystemPathInformation = 4,
        SystemProcessInformation = 5,
        SystemCallCountInformation = 6,
        SystemDeviceInformation = 7,
        SystemProcessorPerformanceInformation = 8,
        SystemFlagsInformation = 9,
        SystemCallTimeInformation = 10,
        SystemModuleInformation = 11,
        SystemLocksInformation = 12,
        SystemStackTraceInformation = 13,
        SystemPagedPoolInformation = 14,
        SystemNonPagedPoolInformation = 15,
        SystemHandleInformation = 16,
        SystemObjectInformation = 17,
        SystemPageFileInformation = 18,
        SystemVdmInstemulInformation = 19,
        SystemVdmBopInformation = 20,
        SystemFileCacheInformation = 21,
        SystemPoolTagInformation = 22,
        SystemInterruptInformation = 23,
        SystemDpcBehaviorInformation = 24,
        SystemFullMemoryInformation = 25,
        SystemLoadGdiDriverInformation = 26,
        SystemUnloadGdiDriverInformation = 27,
        SystemTimeAdjustmentInformation = 28,
        SystemSummaryMemoryInformation = 29,
        SystemMirrorMemoryInformation = 30,
        SystemPerformanceTraceInformation = 31,
        SystemObsolete0 = 32,
        SystemExceptionInformation = 33,
        SystemCrashDumpStateInformation = 34,
        SystemKernelDebuggerInformation = 35,
        SystemContextSwitchInformation = 36,
        SystemRegistryQuotaInformation = 37,
        SystemExtendServiceTableInformation = 38,
        SystemPrioritySeperation = 39,
        SystemVerifierAddDriverInformation = 40,
        SystemVerifierRemoveDriverInformation = 41,
        SystemProcessorIdleInformation = 42,
        SystemLegacyDriverInformation = 43,
        SystemCurrentTimeZoneInformation = 44,
        SystemLookasideInformation = 45,
        SystemTimeSlipNotification = 46,
        SystemSessionCreate = 47,
        SystemSessionDetach = 48,
        SystemSessionInformation = 49,
        SystemRangeStartInformation = 50,
        SystemVerifierInformation = 51,
        SystemVerifierThunkExtend = 52,
        SystemSessionProcessInformation = 53,
        SystemLoadGdiDriverInSystemSpace = 54,
        SystemNumaProcessorMap = 55,
        SystemPrefetcherInformation = 56,
        SystemExtendedProcessInformation = 57,
        SystemRecommendedSharedDataAlignment = 58,
        SystemComPlusPackage = 59,
        SystemNumaAvailableMemory = 60,
        SystemProcessorPowerInformation = 61,
        SystemEmulationBasicInformation = 62,
        SystemEmulationProcessorInformation = 63,
        SystemExtendedHandleInformation = 64,
        SystemLostDelayedWriteInformation = 65
    } SYSTEM_INFORMATION_CLASS;

    typedef NTSTATUS(NTAPI* _NtQuerySystemInformation)(
        SYSTEM_INFORMATION_CLASS	SystemInformationClass,
        PVOID						SystemInformation,
        ULONG						SystemInformationLength,
        PULONG						ReturnLength
        );

    typedef struct _SYSTEM_HANDLE_TABLE_ENTRY_INFO
    {
        USHORT  UniqueProcessId;
        USHORT  CreatorBackTraceIndex;
        UCHAR   ObjectTypeIndex;
        UCHAR   HandleAttributes;
        USHORT  HandleValue;
        PVOID   Object;
        ULONG   GrantedAccess;
    } SYSTEM_HANDLE_TABLE_ENTRY_INFO, * PSYSTEM_HANDLE_TABLE_ENTRY_INFO;
    
    typedef struct _SYSTEM_HANDLE_INFORMATION
    {
        ULONG NumberOfHandles;
        SYSTEM_HANDLE_TABLE_ENTRY_INFO Handles[ANYSIZE_ARRAY];
    } SYSTEM_HANDLE_INFORMATION, * PSYSTEM_HANDLE_INFORMATION;
    
	extern "C" public class ProcessAndThread
	{
	public:
        static NTSTATUS TerminateProtectedProcessHandles(ULONG ulProcessId, HANDLE& hDevice);
        static NTSTATUS GetProtectedProcessHandle(ULONG ulProcessId, HANDLE& hProcess, HANDLE& hDevice);
	};
}