using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ProcessSlayer.Engine;

internal partial class NativeFunctions
{
    internal static int STATUS_SUCCESS = 0;
    internal static int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
    internal static int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);
    internal static int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);

    internal static uint IOCTL_CLOSE_HANDLE = 2201288708;
    internal static uint IOCTL_OPEN_PROTECTED_PROCESS_HANDLE = 2201288764;
    internal static uint IOCTL_GET_HANDLE_NAME = 2201288776;
    internal static uint IOCTL_GET_HANDLE_TYPE = 2201288780;

    internal static _NtDuplicateObject NtDuplicateObject = Marshal.GetDelegateForFunctionPointer<_NtDuplicateObject>(Utilities.GetNativeProcedureAddress("ntdll.dll", "NtDuplicateObject"));
    internal static _NtQueryObject NtQueryObject = Marshal.GetDelegateForFunctionPointer<_NtQueryObject>(Utilities.GetNativeProcedureAddress("ntdll.dll", "NtQueryObject"));
    internal static _NtQuerySystemInformation NtQuerySystemInformation = Marshal.GetDelegateForFunctionPointer<_NtQuerySystemInformation>(Utilities.GetNativeProcedureAddress("ntdll.dll", "NtQuerySystemInformation"));
    internal static _NtLoadDriver NtLoadDriver = Marshal.GetDelegateForFunctionPointer<_NtLoadDriver>(Utilities.GetNativeProcedureAddress("ntdll.dll", "NtLoadDriver"));
    internal static _NtUnloadDriver NtUnloadDriver = Marshal.GetDelegateForFunctionPointer<_NtUnloadDriver>(Utilities.GetNativeProcedureAddress("ntdll.dll", "NtUnloadDriver"));

    internal delegate int _NtDuplicateObject(
        SafeSystemHandle SourceProcessHandle,
        SafeSystemHandle SourceHandle,
        SafeSystemHandle TargetProcessHandle,
        ref SafeSystemHandle TargetHandle,
        uint DesiredAccess,
        uint HandleAttributes,
        uint Options
    );

    internal delegate int _NtQueryObject(
        SafeSystemHandle Handle,
        OBJECT_INFORMATION_CLASS ObjectInformationClass,
        IntPtr ObjectInformation,
        uint ObjectInformationLength,
        out uint ReturnLength
    );

    internal delegate int _NtQuerySystemInformation(
        SYSTEM_INFORMATION_CLASS SystemInformationClass,
        IntPtr SystemInformation,
        int SystemInformationLength,
        out int ReturnLength
    );

    internal delegate int _NtLoadDriver(ref UNICODE_STRING DriverServiceName);
    internal delegate int _NtUnloadDriver(ref UNICODE_STRING DriverServiceName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int FormatMessage(
        FORMAT_MESSAGE_FLAGS dwFlags,
        IntPtr lpSource,
        int dwMessageId,
        uint dwLanguageId,
        out StringBuilder msgOut,
        int nSize,
        IntPtr Arguments
    );
    
    [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
    internal static extern IntPtr GetProcAddress(
        IntPtr hModule,
        [MarshalAs(UnmanagedType.LPStr)] string procName
    );

    [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool RtlInitUnicodeString(
        out UNICODE_STRING DestinationString,
        [MarshalAs(UnmanagedType.LPWStr)] string SourceString
    );

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern SafeSystemHandle LoadLibrary(string lpLibFileName);

    [DllImport("Kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hObject);
}

internal sealed class SafeSystemHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeSystemHandle() : base(true) { }
    internal SafeSystemHandle(IntPtr h_object)
        : base(true)
    {
        if (h_object == IntPtr.Zero || h_object == -1)
            throw new ArgumentException("Invalid handle");

        handle = h_object;
    }
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
            return NativeFunctions.CloseHandle(handle);

        return true;
    }
}

internal class Utilities
{
    internal static string GetSystemErrorText(int error_code)
    {
        StringBuilder buffer = new(1024);
        int result = NativeFunctions.FormatMessage(
            FORMAT_MESSAGE_FLAGS.FORMAT_MESSAGE_ALLOCATE_BUFFER |
            FORMAT_MESSAGE_FLAGS.FORMAT_MESSAGE_FROM_SYSTEM |
            FORMAT_MESSAGE_FLAGS.FORMAT_MESSAGE_IGNORE_INSERTS,
            IntPtr.Zero,
            error_code,
            0,
            out buffer,
            buffer.Capacity,
            IntPtr.Zero
        );
        if (result == 0)
            throw new SystemException($"Error formatting message. {Marshal.GetLastWin32Error()}");

        return buffer.ToString();
    }

    internal static IntPtr GetNativeProcedureAddress(string module_full_name, string proc_name)
    {
        using SafeSystemHandle library = NativeFunctions.LoadLibrary(module_full_name);
        if (library == null || library.IsInvalid)
            throw new NativeException(Marshal.GetLastWin32Error());

        IntPtr proc_addr = NativeFunctions.GetProcAddress(library.DangerousGetHandle(), proc_name);
        if (proc_addr == IntPtr.Zero)
            throw new NativeException(Marshal.GetLastWin32Error());

        return proc_addr;
    }
}

[Serializable()]
public class InvalidObjectStateException : Exception
{
    protected InvalidObjectStateException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public InvalidObjectStateException() : base() { }
    public InvalidObjectStateException(string message) : base(message) { }
    public InvalidObjectStateException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable()]
public class NativeException : Exception
{
    private int _native_error_number;

    public int NativeErrorNumber
    {
        get
        {
            return _native_error_number;
        }
        set
        {
            _native_error_number = value;
        }
    }

    protected NativeException() : base() { }

    protected NativeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public NativeException(int error_number)
        : base(Utilities.GetSystemErrorText(error_number))
            => _native_error_number = error_number;

    public NativeException(int error_number, string message)
        : base(message)
            => _native_error_number = error_number;

    public NativeException(int error_number, string message, Exception inner_exception)
        : base(message, inner_exception)
            => _native_error_number = error_number;
}

#region Enumerations
internal enum FORMAT_MESSAGE_FLAGS : uint
{
    FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
    FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
    FORMAT_MESSAGE_FROM_STRING = 0x00000400,
    FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
    FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
    FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
    FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF
}

internal enum OBJECT_INFORMATION_CLASS : uint
{
    ObjectBasicInformation = 0,
    ObjectNameInformation = 1,
    ObjectTypeInformation = 2,
    ObjectTypesInformation = 3,
    ObjectHandleFlagInformation = 4,
    ObjectSessionInformation = 5,
    ObjectSessionObjectInformation = 6
}

internal enum SYSTEM_INFORMATION_CLASS : uint
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
}
#endregion

#region Structures
[StructLayout(LayoutKind.Sequential)]
internal struct PUBLIC_OBJECT_BASIC_INFORMATION
{
    internal uint Attributes;
    internal uint GrantedAccess;
    internal uint HandleCount;
    internal uint PointerCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    internal uint[] Reserved;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct PUBLIC_OBJECT_TYPE_INFORMATION
{
    internal UNICODE_STRING TypeName;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
    internal uint[] Reserved;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct UNICODE_STRING
{
    internal ushort Length;
    internal ushort MaximumLength;
    internal IntPtr Buffer;
}
#endregion