using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Win32.SafeHandles;

namespace ProcessSlayer.Engine;

public class IO
{
    internal static SafeFileHandle CreateGenericDeviceFile(string device_name)
    {
        SafeFileHandle file_handle = NativeFunctions.CreateFile(
            $"\\\\.\\{device_name}",
            (FILE_SECURITY)ACCESS_TYPE.GENERIC_ALL,
            FILE_SHARE_MODE.FILE_SHARE_NONE,
            IntPtr.Zero,
            FILE_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            new()
        );
        if (file_handle is null || file_handle.IsInvalid)
            throw new NativeException(Marshal.GetLastWin32Error());

        return file_handle;
    }
}

internal partial class NativeFunctions
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "CreateFileA")]
    internal static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPStr)] string lpFileName,
        FILE_SECURITY dwDesiredAccess,
        FILE_SHARE_MODE dwShareMode,
        [In][Optional] IntPtr lpSecurityAttributes,
        FILE_DISPOSITION dwCreateDisposition,
        FILE_FLAGS_AND_ATTRIBUTES dwFlagsAndAttributes,
        [In][Optional] SafeFileHandle hTemplateFile
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        ref IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        ref IntPtr lpOverlapped
    );
}

#region Enumerations
internal enum FILE_SECURITY : uint
{
    FILE_READ_DATA = 0x0001,
    FILE_LIST_DIRECTORY = 0x0001,
    FILE_WRITE_DATA = 0x0002,
    FILE_ADD_FILE = 0x0002,
    FILE_APPEND_DATA = 0x0004,
    FILE_ADD_SUBDIRECTORY = 0x0004,
    FILE_CREATE_PIPE_INSTANCE = 0x0004,
    FILE_READ_EA = 0x0008,
    FILE_WRITE_EA = 0x0010,
    FILE_EXECUTE = 0x0020,
    FILE_TRAVERSE = 0x0020,
    FILE_DELETE_CHILD = 0x0040,
    FILE_READ_ATTRIBUTES = 0x0080,
    FILE_WRITE_ATTRIBUTES = 0x0100,
    FILE_ALL_ACCESS = ACCESS_TYPE.STANDARD_RIGHTS_REQUIRED | ACCESS_TYPE.SYNCHRONIZE | 0x1FF,
    FILE_GENERIC_READ = ACCESS_TYPE.STANDARD_RIGHTS_READ |
                        FILE_READ_DATA |
                        FILE_READ_ATTRIBUTES |
                        FILE_READ_EA |
                        ACCESS_TYPE.SYNCHRONIZE,

    FILE_GENERIC_WRITE = ACCESS_TYPE.STANDARD_RIGHTS_WRITE |
                         FILE_WRITE_DATA |
                         FILE_WRITE_ATTRIBUTES |
                         FILE_WRITE_EA |
                         FILE_APPEND_DATA |
                         ACCESS_TYPE.SYNCHRONIZE,

    FILE_GENERIC_EXECUTE = ACCESS_TYPE.STANDARD_RIGHTS_EXECUTE |
                           FILE_READ_ATTRIBUTES |
                           FILE_EXECUTE |
                           ACCESS_TYPE.SYNCHRONIZE
}

[Flags]
internal enum FILE_SHARE_MODE : uint
{
    FILE_SHARE_NONE = 0,
    FILE_SHARE_READ = 0x00000001,
    FILE_SHARE_WRITE = 0x00000002,
    FILE_SHARE_DELETE = 0x00000004
}

[Flags]
internal enum FILE_FLAGS_AND_ATTRIBUTES : uint
{
    FILE_ATTRIBUTE_READONLY = 0x00000001,
    FILE_ATTRIBUTE_HIDDEN = 0x00000002,
    FILE_ATTRIBUTE_SYSTEM = 0x00000004,
    FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
    FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
    FILE_ATTRIBUTE_DEVICE = 0x00000040,
    FILE_ATTRIBUTE_NORMAL = 0x00000080,
    FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
    FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
    FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
    FILE_ATTRIBUTE_COMPRESSED = 0x00000800,
    FILE_ATTRIBUTE_OFFLINE = 0x00001000,
    FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
    FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
    FILE_ATTRIBUTE_INTEGRITY_STREAM = 0x00008000,
    FILE_ATTRIBUTE_VIRTUAL = 0x00010000,
    FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x00020000,
    FILE_ATTRIBUTE_EA = 0x00040000,
    FILE_ATTRIBUTE_PINNED = 0x00080000,
    FILE_ATTRIBUTE_UNPINNED = 0x00100000,
    FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x00040000,
    FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000,
    TREE_CONNECT_ATTRIBUTE_PRIVACY = 0x00004000,
    TREE_CONNECT_ATTRIBUTE_INTEGRITY = 0x00008000,
    TREE_CONNECT_ATTRIBUTE_GLOBAL = 0x00000004,
    TREE_CONNECT_ATTRIBUTE_PINNED = 0x00000002,
    FILE_ATTRIBUTE_STRICTLY_SEQUENTIAL = 0x20000000,

    FILE_FLAG_WRITE_THROUGH = 0x80000000,
    FILE_FLAG_OVERLAPPED = 0x40000000,
    FILE_FLAG_NO_BUFFERING = 0x20000000,
    FILE_FLAG_RANDOM_ACCESS = 0x10000000,
    FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
    FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
    FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
    FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
    FILE_FLAG_SESSION_AWARE = 0x00800000,
    FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
    FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
    FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000,

    SECURITY_ANONYMOUS = SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous << 16,
    SECURITY_IDENTIFICATION = SECURITY_IMPERSONATION_LEVEL.SecurityIdentification << 16,
    SECURITY_IMPERSONATION = SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation << 16,
    SECURITY_DELEGATION = SECURITY_IMPERSONATION_LEVEL.SecurityDelegation << 16,
    SECURITY_CONTEXT_TRACKING = 0x00040000,
    SECURITY_EFFECTIVE_ONLY = 0x00080000,
    SECURITY_SQOS_PRESENT = 0x00100000,
    SECURITY_VALID_SQOS_FLAGS = 0x001F0000,
}

internal enum FILE_DISPOSITION : uint
{
    CREATE_NEW = 1,
    CREATE_ALWAYS = 2,
    OPEN_EXISTING = 3,
    OPEN_ALWAYS = 4,
    TRUNCATE_EXISTING = 5
}
#endregion

#region Structures
/*
clrjit!_OVERLAPPED
   +0x000 Internal         : Uint8B
   +0x008 InternalHigh     : Uint8B
   +0x010 Offset           : Uint4B
   +0x014 OffsetHigh       : Uint4B
   +0x010 Pointer          : Ptr64 Void
   +0x018 hEvent           : Ptr64 Void
*/
[StructLayout(LayoutKind.Explicit)]
internal struct OVERLAPPED
{
    [FieldOffset(0)] internal ulong Internal;
    [FieldOffset(8)] internal ulong InternalHigh;
    [FieldOffset(10)] internal union_OVERLAPPED OffsetInformation;
    [FieldOffset(10)] internal IntPtr Pointer;
    [FieldOffset(18)] internal IntPtr hEvent;
}
[StructLayout(LayoutKind.Sequential)]
internal struct union_OVERLAPPED
{
    internal uint Offset;
    internal uint OffsetHigh;
}
#endregion