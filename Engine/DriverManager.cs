using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ProcessSlayer.Engine;

public class DriverManager : IDisposable
{
    private readonly string _registry_path;
    private readonly SafeFileHandle _driver_handle;
    private readonly bool _is_connected_only;
    private bool _is_loaded;

    public string Path { get; }
    public string ServiceName { get; }
    public SafeFileHandle Device { get { return _driver_handle; } }
    public bool IsConnected { get { return !_driver_handle.IsInvalid && _is_connected_only; } }

    public DriverManager(string path, string service_name)
    {
        _is_loaded = false;
        _is_connected_only = false;
        Path = path;
        ServiceName = service_name;
        _registry_path = $"SYSTEM\\CurrentControlSet\\Services\\{service_name}";

        LoadDriver();
        _driver_handle = ConnectToDriver();
        if (_driver_handle.IsInvalid)
            throw new InvalidObjectStateException("'ConnectToDriver' returned an invalid handle.");

        _is_connected_only = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _driver_handle.Dispose();
            UnloadDriver();
        }
    }

    private void LoadDriver()
    {
        AccessControl.AdjustCurrentTokenPrivileges("SeLoadDriverPrivilege");
        AccessControl.AdjustCurrentTokenPrivileges("SeSecurityPrivilege");
        AccessControl.AdjustCurrentTokenPrivileges("SeDebugPrivilege");
        AccessControl.AdjustCurrentTokenPrivileges("SeTcbPrivilege");
        AccessControl.AdjustCurrentTokenPrivileges("SeIncreaseQuotaPrivilege");
        SetRegistryValues();

        NativeFunctions.RtlInitUnicodeString(out UNICODE_STRING driver_path, $"\\Registry\\Machine\\{_registry_path}");
        if (driver_path.Buffer == IntPtr.Zero)
            throw new InvalidObjectStateException("'RtlInitUnicodeString' returned a null or empty string.");

        int load_result = NativeFunctions.NtLoadDriver(ref driver_path);
        if (load_result != NativeFunctions.STATUS_SUCCESS)
            throw new NativeException(load_result, $"'NtLoadDriver' returned {load_result}.");

        // Not because we are trying to kill protected processes that we want someone using our token for bad things.
        AccessControl.AdjustCurrentTokenPrivileges("SeLoadDriverPrivilege", true);
        AccessControl.AdjustCurrentTokenPrivileges("SeSecurityPrivilege", true);

        _is_loaded = true;
    }

    private void UnloadDriver()
    {
        AccessControl.AdjustCurrentTokenPrivileges("SeLoadDriverPrivilege");
        AccessControl.AdjustCurrentTokenPrivileges("SeSecurityPrivilege");

        NativeFunctions.RtlInitUnicodeString(out UNICODE_STRING driver_path, $"\\Registry\\Machine\\{_registry_path}");
        if (driver_path.Buffer == IntPtr.Zero)
            throw new InvalidObjectStateException("'RtlInitUnicodeString' returned a null or empty string.");

        int unload_result = NativeFunctions.NtUnloadDriver(ref driver_path);
        if (unload_result != NativeFunctions.STATUS_SUCCESS)
            throw new NativeException(unload_result, $"'NtUnloadDriver' returned {unload_result}.");

        AccessControl.AdjustCurrentTokenPrivileges("SeLoadDriverPrivilege", true);
        AccessControl.AdjustCurrentTokenPrivileges("SeSecurityPrivilege", true);
        RemoveRegistryKey();

        _is_loaded = false;
    }

    private void SetRegistryValues()
    {
        using RegistryKey root_key = Registry.LocalMachine;
        
        using RegistryKey? temp_svc_key = root_key.OpenSubKey(_registry_path, true);
        using RegistryKey service_key = temp_svc_key is null ? root_key.CreateSubKey(_registry_path) : temp_svc_key;
        
        service_key.SetValue("Type", 0, RegistryValueKind.DWord);
        service_key.SetValue("ErrorControl", 0, RegistryValueKind.DWord);
        service_key.SetValue("Start", 0, RegistryValueKind.DWord);
        service_key.SetValue("ImagePath", $"\\??\\{Path}", RegistryValueKind.String);
    }

    private void RemoveRegistryKey()
    {
        using RegistryKey root_key = Registry.LocalMachine;
        
        using RegistryKey? temp_svc_key = root_key.OpenSubKey(_registry_path, true);
        root_key?.DeleteSubKey(_registry_path);
    }

    private SafeFileHandle ConnectToDriver()
    {
        if (!_is_loaded)
            throw new InvalidObjectStateException("Cannot connect to driver. Driver is not loaded.");
        
        return IO.CreateGenericDeviceFile("PROCEXP152");
    }
}