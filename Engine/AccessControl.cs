using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ProcessSlayer.Engine;

public class AccessControl
{
    public static void AdjustCurrentTokenPrivileges(string privilege_name, bool remove = false)
    {
        if (!NativeFunctions.OpenProcessToken(NativeFunctions.GetCurrentProcess(), TOKEN_ACCESS_RIGHT.TOKEN_QUERY | TOKEN_ACCESS_RIGHT.TOKEN_ADJUST_PRIVILEGES, out SafeAccessTokenHandle hToken))
            throw new NativeException(Marshal.GetLastWin32Error());

        try
        {
            TOKEN_PRIVILEGE privilege = new();
            if (!NativeFunctions.LookupPrivilegeValue(string.Empty, privilege_name, ref privilege.Privilege.Luid))
                throw new NativeException(Marshal.GetLastWin32Error());

            privilege.PrivilegeCount = 1;
            if (remove)
                privilege.Privilege.Attributes = PRIVILEGE_ATTRIBUTE.SE_PRIVILEGE_REMOVED;
            else
                privilege.Privilege.Attributes = PRIVILEGE_ATTRIBUTE.SE_PRIVILEGE_ENABLED;

            if (!NativeFunctions.AdjustTokenPrivileges(hToken, false, ref privilege, 0, IntPtr.Zero, IntPtr.Zero))
                throw new NativeException(Marshal.GetLastWin32Error());
        }
        finally
        {
            hToken.Dispose();
        }
    }
}

internal partial class NativeFunctions
{
    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool OpenProcessToken(
        SafeSystemHandle ProcessHandle,
        TOKEN_ACCESS_RIGHT DesiredAccess,
        out SafeAccessTokenHandle pHandle
    );

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW")]
    internal static extern bool LookupPrivilegeValue(
        string lpSystemName,
        string lpName,
        ref LUID lpLuid
    );

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool AdjustTokenPrivileges(
        SafeAccessTokenHandle TokenHandle,
        bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGE NewState,
        uint BufferLength,
        ref TOKEN_PRIVILEGE PreviousState,
        ref uint ReturnLength
    );

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool AdjustTokenPrivileges(
        SafeAccessTokenHandle TokenHandle,
        bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGE NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength
    );
}

#region Enumerations
internal enum ACCESS_TYPE : uint
{
    DELETE = 0x00010000,
    READ_CONTROL = 0x00020000,
    WRITE_DAC = 0x00040000,
    WRITE_OWNER = 0x00080000,
    SYNCHRONIZE = 0x00100000,
    STANDARD_RIGHTS_REQUIRED = 0x000F0000,
    STANDARD_RIGHTS_READ = READ_CONTROL,
    STANDARD_RIGHTS_WRITE = READ_CONTROL,
    STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
    STANDARD_RIGHTS_ALL = 0x001F0000,
    SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
    ACCESS_SYSTEM_SECURITY = 0x01000000,
    MAXIMUM_ALLOWED = 0x02000000,
    GENERIC_READ = 0x80000000,
    GENERIC_WRITE = 0x40000000,
    GENERIC_EXECUTE = 0x20000000,
    GENERIC_ALL = 0x10000000
}

internal enum TOKEN_ACCESS_RIGHT : uint
{
    TOKEN_ASSIGN_PRIMARY = 0x0001,
    TOKEN_DUPLICATE = 0x0002,
    TOKEN_IMPERSONATE = 0x0004,
    TOKEN_QUERY = 0x0008,
    TOKEN_QUERY_SOURCE = 0x0010,
    TOKEN_ADJUST_PRIVILEGES = 0x0020,
    TOKEN_ADJUST_GROUPS = 0x0040,
    TOKEN_ADJUST_DEFAULT = 0x0080,
    TOKEN_ADJUST_SESSIONID = 0x0100,
    TOKEN_ALL_ACCESS_P = ACCESS_TYPE.STANDARD_RIGHTS_REQUIRED |
                         TOKEN_ASSIGN_PRIMARY |
                         TOKEN_DUPLICATE |
                         TOKEN_IMPERSONATE |
                         TOKEN_QUERY |
                         TOKEN_QUERY_SOURCE |
                         TOKEN_ADJUST_PRIVILEGES |
                         TOKEN_ADJUST_GROUPS |
                         TOKEN_ADJUST_DEFAULT,

    TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID,
    TOKEN_READ = ACCESS_TYPE.STANDARD_RIGHTS_READ | TOKEN_QUERY,
    TOKEN_WRITE = ACCESS_TYPE.STANDARD_RIGHTS_WRITE |
                  TOKEN_ADJUST_PRIVILEGES |
                  TOKEN_ADJUST_GROUPS |
                  TOKEN_ADJUST_DEFAULT,

    TOKEN_EXECUTE = ACCESS_TYPE.STANDARD_RIGHTS_EXECUTE,
    TOKEN_TRUST_CONSTRAINT_MASK = ACCESS_TYPE.STANDARD_RIGHTS_READ |
                                  TOKEN_QUERY |
                                  TOKEN_QUERY_SOURCE,

    TOKEN_TRUST_ALLOWED_MASK = TOKEN_TRUST_CONSTRAINT_MASK |
                               TOKEN_DUPLICATE |
                               TOKEN_IMPERSONATE,
}

internal enum PRIVILEGE_ATTRIBUTE : uint
{
    SE_PRIVILEGE_NONE = 0x00000000,
    SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
    SE_PRIVILEGE_ENABLED = 0x00000002,
    SE_PRIVILEGE_REMOVED = 0X00000004,
    SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000,
    SE_PRIVILEGE_VALID_ATTRIBUTES = SE_PRIVILEGE_ENABLED_BY_DEFAULT |
                                    SE_PRIVILEGE_ENABLED |
                                    SE_PRIVILEGE_REMOVED |
                                    SE_PRIVILEGE_USED_FOR_ACCESS
}

internal enum SECURITY_IMPERSONATION_LEVEL {
    SecurityAnonymous,
    SecurityIdentification,
    SecurityImpersonation,
    SecurityDelegation
}
#endregion

#region Structures
[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    internal uint LowPart;
    internal long HighPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LUID_AND_ATTRIBUTES
{
    internal LUID Luid;
    internal PRIVILEGE_ATTRIBUTE Attributes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TOKEN_PRIVILEGE
{
    internal uint PrivilegeCount;
    internal LUID_AND_ATTRIBUTES Privilege;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SECURITY_ATTRIBUTES
{
    internal uint nLength;
    internal IntPtr lpSecurityDescriptor;
    internal bool bInheritHandle;
}
#endregion