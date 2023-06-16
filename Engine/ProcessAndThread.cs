using System.Runtime.InteropServices;

namespace ProcessSlayer.Engine;

public class ProcessAndThread
{
    internal static SafeSystemHandle OpenProtectedProcess(long process_id, ref DriverManager driver_manager)
    {
        if (!driver_manager.IsConnected)
            throw new ArgumentException("Driver manager is not connected.");

        IntPtr in_buffer = Marshal.AllocHGlobal(8);
        IntPtr h_process = IntPtr.Zero;
        IntPtr overlapped = IntPtr.Zero;
        try
        {
            Marshal.WriteInt64(in_buffer, process_id);
            if (!NativeFunctions.DeviceIoControl(
                driver_manager.Device,
                NativeFunctions.IOCTL_OPEN_PROTECTED_PROCESS_HANDLE,
                in_buffer,
                8,
                ref h_process,
                (uint)IntPtr.Size,
                out uint bytes_returned,
                ref overlapped
            ) || bytes_returned == 0)
                throw new InvalidObjectStateException("Failed getting handle to process. Either 'DeviceIoControl' failed, or the driver didn't returned any data.");

            if (h_process == IntPtr.Zero || h_process == -1)
                throw new InvalidObjectStateException("Driver returned an invalid handle.");

            return new SafeSystemHandle(h_process);
        }
        finally
        {
            Marshal.FreeHGlobal(in_buffer);
        }
    }

    public static void TerminateProtectedProcessHandles(ulong process_id, ref DriverManager driver_manager)
    {
        if (!driver_manager.IsConnected)
            throw new ArgumentException("Driver manager is not connected.");

        IO_CONTROL control = new() { ulPid = process_id };
        foreach (SYSTEM_HANDLE_TABLE_ENTRY_INFO handle_entry in GetProcessHandleInformation())
        {
            if (handle_entry.UniqueProcessId == process_id)
            {
                control.ulSize = 0;
                control.ulHandle = handle_entry.HandleValue;
                control.lpObjectAddress = handle_entry.Object;

                IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IO_CONTROL)));
                try
                {
                    Marshal.StructureToPtr(control, buffer, false);
                    IntPtr dummy_ptr = IntPtr.Zero;
                    if (!NativeFunctions.DeviceIoControl(
                        driver_manager.Device,
                        NativeFunctions.IOCTL_CLOSE_HANDLE,
                        buffer,
                        (uint)Marshal.SizeOf(typeof(IO_CONTROL)),
                        ref dummy_ptr,
                        0,
                        out uint _,
                        ref dummy_ptr
                    ))
                        throw new InvalidObjectStateException("Close protected handle operation failed.");
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }

    internal static List<SYSTEM_HANDLE_TABLE_ENTRY_INFO> GetProcessHandleInformation()
    {
        IntPtr buffer;
        int result;
        int bytes_needed = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO)) * 100;
        do
        {
            buffer = Marshal.AllocHGlobal(bytes_needed);
            result = NativeFunctions.NtQuerySystemInformation(
                SYSTEM_INFORMATION_CLASS.SystemHandleInformation,
                buffer,
                bytes_needed,
                out bytes_needed
            );
            if (result == NativeFunctions.STATUS_SUCCESS)
                break;
            if (
                result != NativeFunctions.STATUS_BUFFER_TOO_SMALL &&
                result != NativeFunctions.STATUS_INFO_LENGTH_MISMATCH &&
                result != NativeFunctions.STATUS_BUFFER_OVERFLOW
            )
                throw new NativeException(result, $"'NtQuerySystemInformation' returned {result}.");

            Marshal.FreeHGlobal(buffer);

        } while (
            result == NativeFunctions.STATUS_BUFFER_TOO_SMALL ||
            result == NativeFunctions.STATUS_INFO_LENGTH_MISMATCH ||
            result == NativeFunctions.STATUS_BUFFER_OVERFLOW
        );

        byte[] count_buffer = new byte[4];
        Marshal.Copy(buffer, count_buffer, 0, 4);
        int entry_count = BitConverter.ToInt32(count_buffer);

        List<SYSTEM_HANDLE_TABLE_ENTRY_INFO> handle_table = new();
        IntPtr list_start = buffer + 8;
        int entry_size = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO));
        for (int i = 0; i < entry_count; i++)
        {
            SYSTEM_HANDLE_TABLE_ENTRY_INFO? current = (SYSTEM_HANDLE_TABLE_ENTRY_INFO?)Marshal.PtrToStructure(list_start, typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO));
            if (current is not null)
                handle_table.Add((SYSTEM_HANDLE_TABLE_ENTRY_INFO)current);

            list_start += entry_size;
        }

        return handle_table;
    }
}

internal partial class NativeFunctions
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern SafeSystemHandle GetCurrentProcess();
}

#region Structures
[StructLayout(LayoutKind.Sequential)]
internal struct IO_CONTROL
{
    internal ulong ulPid;
    internal IntPtr lpObjectAddress;
    internal ulong ulSize;
    internal ulong ulHandle;
}

[StructLayout(LayoutKind.Explicit)]
internal struct SYSTEM_HANDLE_TABLE_ENTRY_INFO
{
    [FieldOffset(0)] internal ushort UniqueProcessId;
    [FieldOffset(2)] internal ushort CreatorBackTraceIndex;

    [MarshalAs(UnmanagedType.U1)]
    [FieldOffset(4)] internal byte ObjectTypeIndex;

    [MarshalAs(UnmanagedType.U1)]
    [FieldOffset(5)] internal byte HandleAttributes;
    [FieldOffset(6)] internal ushort HandleValue;
    [FieldOffset(8)] internal IntPtr Object;
    [FieldOffset(10)] internal uint GrantedAccess;
}

[StructLayout(LayoutKind.Explicit)]
internal struct SYSTEM_HANDLE_INFORMATION
{
    [FieldOffset(0)] internal uint NumberOfHandles;
    [FieldOffset(8)] internal IntPtr Handles;
}
#endregion