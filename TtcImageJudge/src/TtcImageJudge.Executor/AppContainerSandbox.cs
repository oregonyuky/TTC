using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace TtcImageJudge.Executor;

public sealed class SandboxException(string message, Exception? inner = null) : Exception(message, inner);

public sealed record SandboxProcessResult(int ExitCode, bool TimedOut, bool MemoryLimitExceeded,
    ulong PeakMemoryBytes, string Diagnostic);

public sealed class AppContainerSandbox : IDisposable
{
    private const uint CreateSuspended = 0x00000004;
    private const uint CreateUnicodeEnvironment = 0x00000400;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private const uint JobObjectLimitActiveProcess = 0x00000008;
    private const uint JobObjectLimitJobMemory = 0x00000200;
    private const uint JobObjectLimitKillOnJobClose = 0x00002000;
    private static readonly IntPtr ProcThreadAttributeSecurityCapabilities = (IntPtr)0x00020009;
    private const uint DaclSecurityInformation = 0x00000004;
    private const uint WindowStationAllAccess = 0x0000037F;
    private const uint DesktopAllAccess = 0x000001FF;
    private const uint ReadControlWriteDac = 0x00060000;
    private static readonly object WindowObjectAclLock = new();

    private readonly string _profileName;
    private readonly string _taskDirectory;
    private readonly SecurityIdentifier _sid;
    private readonly IntPtr _windowStation;
    private readonly IntPtr _desktop;
    private readonly string _desktopPath;
    private bool _disposed;

    public static bool NativeApisAvailable
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            if (!NativeLibrary.TryLoad("userenv.dll", out var library)) return false;
            NativeLibrary.Free(library);
            return true;
        }
    }

    private AppContainerSandbox(string profileName, string taskDirectory, SecurityIdentifier sid,
        IntPtr windowStation, IntPtr desktop, string desktopPath)
    {
        _profileName = profileName;
        _taskDirectory = taskDirectory;
        _sid = sid;
        _windowStation = windowStation;
        _desktop = desktop;
        _desktopPath = desktopPath;
    }

    public static AppContainerSandbox Create(string profileName, string taskDirectory)
    {
        EnsureLowBoxConsoleSupport();
        IntPtr sidPointer = IntPtr.Zero;
        try
        {
            var result = Native.CreateAppContainerProfile(profileName, "TTC Image Judge",
                "Sandbox descartável para uma submissão", IntPtr.Zero, 0, out sidPointer);
            if (result != 0) throw new SandboxException($"CreateAppContainerProfile falhou (HRESULT 0x{result:X8}).");
            var sid = new SecurityIdentifier(sidPointer);
            GrantTaskDirectory(taskDirectory, sid);
            var currentWindowStation = Native.GetProcessWindowStation();
            if (currentWindowStation == IntPtr.Zero)
                ThrowLastError("Localizar window station/desktop");
            var windowStationName = GetWindowObjectName(currentWindowStation);
            var windowStation = Native.OpenWindowStation(windowStationName, false,
                ReadControlWriteDac | 0x0003);
            if (windowStation == IntPtr.Zero) ThrowLastError("Abrir window station para configurar o LowBox");
            var desktopName = "TtcImageJudge_" + profileName[(profileName.LastIndexOf('.') + 1)..];
            var desktop = Native.CreateDesktop(desktopName, null, IntPtr.Zero, 0, 0x000F01FF, IntPtr.Zero);
            if (desktop == IntPtr.Zero)
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Native.CloseWindowStation(windowStation);
                throw new SandboxException("Criar desktop privado do LowBox falhou: " + error);
            }
            lock (WindowObjectAclLock)
            {
                try
                {
                    ChangeWindowObjectAccess(windowStation, sid, WindowStationAllAccess, grant: true);
                    ChangeWindowObjectAccess(desktop, sid, DesktopAllAccess, grant: true);
                }
                catch
                {
                    try { ChangeWindowObjectAccess(windowStation, sid, 0, grant: false); } catch { }
                    Native.CloseDesktop(desktop);
                    Native.CloseWindowStation(windowStation);
                    throw;
                }
            }
            return new AppContainerSandbox(profileName, taskDirectory, sid, windowStation, desktop,
                windowStationName + "\\" + desktopName);
        }
        catch
        {
            Native.DeleteAppContainerProfile(profileName);
            throw;
        }
        finally
        {
            if (sidPointer != IntPtr.Zero) Native.FreeSid(sidPointer);
        }
    }

    private static void EnsureLowBoxConsoleSupport()
    {
        try
        {
            using var console = Registry.CurrentUser.CreateSubKey("Console", writable: true);
            console?.SetValue("LowBoxConsoleEnabled", 1, RegistryValueKind.DWord);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            throw new SandboxException("Não foi possível habilitar processos de console LowBox para o usuário atual.", exception);
        }
    }

    public Task<SandboxProcessResult> RunProcessAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout,
        int memoryLimitMb, int processLimit, CancellationToken cancellationToken) =>
        Task.Run(() => RunProcess(executable, arguments, timeout, memoryLimitMb, processLimit, cancellationToken),
            cancellationToken);

    private SandboxProcessResult RunProcess(string executable, IReadOnlyList<string> arguments, TimeSpan timeout,
        int memoryLimitMb, int processLimit, CancellationToken cancellationToken)
    {
        executable = Path.GetFullPath(executable);
        if (!executable.StartsWith(_taskDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new SandboxException("O executável deve estar dentro do diretório exclusivo da tarefa.");
        if (!File.Exists(executable)) throw new SandboxException("Executável isolado não encontrado.");

        IntPtr sidPointer = IntPtr.Zero, attributeList = IntPtr.Zero, capabilitiesPointer = IntPtr.Zero;
        IntPtr environmentPointer = IntPtr.Zero, job = IntPtr.Zero;
        Native.ProcessInformation process = default;
        try
        {
            sidPointer = Marshal.AllocHGlobal(_sid.BinaryLength);
            var sidBytes = new byte[_sid.BinaryLength];
            _sid.GetBinaryForm(sidBytes, 0);
            Marshal.Copy(sidBytes, 0, sidPointer, sidBytes.Length);

            var capabilities = new Native.SecurityCapabilities
            {
                AppContainerSid = sidPointer,
                Capabilities = IntPtr.Zero,
                CapabilityCount = 0,
                Reserved = 0
            };
            capabilitiesPointer = Marshal.AllocHGlobal(Marshal.SizeOf<Native.SecurityCapabilities>());
            Marshal.StructureToPtr(capabilities, capabilitiesPointer, false);

            nuint attributeSize = 0;
            Native.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeSize);
            attributeList = Marshal.AllocHGlobal(checked((int)attributeSize));
            if (!Native.InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeSize))
                ThrowLastError("InitializeProcThreadAttributeList");
            if (!Native.UpdateProcThreadAttribute(attributeList, 0, ProcThreadAttributeSecurityCapabilities,
                    capabilitiesPointer, (nuint)Marshal.SizeOf<Native.SecurityCapabilities>(), IntPtr.Zero, IntPtr.Zero))
                ThrowLastError("UpdateProcThreadAttribute");

            var startup = new Native.StartupInfoEx
            {
                StartupInfo = new Native.StartupInfo
                {
                    Cb = Marshal.SizeOf<Native.StartupInfoEx>(),
                    Desktop = _desktopPath
                },
                AttributeList = attributeList
            };
            var environment = BuildEnvironmentBlock();
            environmentPointer = Marshal.StringToHGlobalUni(environment);
            var commandLine = new StringBuilder(Quote(executable));
            foreach (var argument in arguments) commandLine.Append(' ').Append(Quote(argument));

            if (!Native.CreateProcess(executable, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                    CreateSuspended | CreateUnicodeEnvironment | ExtendedStartupInfoPresent,
                    environmentPointer, _taskDirectory, ref startup, out process))
                ThrowLastError("CreateProcess(AppContainer)");

            job = Native.CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) ThrowLastError("CreateJobObject");
            ConfigureJob(job, memoryLimitMb, processLimit);
            if (!Native.AssignProcessToJobObject(job, process.ProcessHandle))
                ThrowLastError("AssignProcessToJobObject");
            if (Native.ResumeThread(process.ThreadHandle) == uint.MaxValue) ThrowLastError("ResumeThread");

            var started = Stopwatch.GetTimestamp();
            var timedOut = false;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var wait = Native.WaitForSingleObject(process.ProcessHandle, 50);
                if (wait == WaitObject0) break;
                if (wait != WaitTimeout) ThrowLastError("WaitForSingleObject");
                if (Stopwatch.GetElapsedTime(started) < timeout) continue;
                timedOut = true;
                Native.TerminateJobObject(job, 1460);
                Native.WaitForSingleObject(process.ProcessHandle, 5_000);
                break;
            }

            if (!Native.GetExitCodeProcess(process.ProcessHandle, out var exitCode)) ThrowLastError("GetExitCodeProcess");
            var peak = QueryPeakMemory(job);
            var limitBytes = checked((ulong)memoryLimitMb * 1024 * 1024);
            var memoryExceeded = !timedOut && peak >= limitBytes && !File.Exists(Path.Combine(_taskDirectory, "result.json"));
            return new SandboxProcessResult(unchecked((int)exitCode), timedOut, memoryExceeded, peak,
                $"AppContainer={_profileName}; PeakJobMemory={peak}; ExitCode=0x{exitCode:X8}");
        }
        catch (OperationCanceledException)
        {
            if (job != IntPtr.Zero) Native.TerminateJobObject(job, 1223);
            throw;
        }
        finally
        {
            if (job != IntPtr.Zero) Native.CloseHandle(job);
            if (process.ThreadHandle != IntPtr.Zero) Native.CloseHandle(process.ThreadHandle);
            if (process.ProcessHandle != IntPtr.Zero) Native.CloseHandle(process.ProcessHandle);
            if (attributeList != IntPtr.Zero) Native.DeleteProcThreadAttributeList(attributeList);
            if (environmentPointer != IntPtr.Zero) Marshal.FreeHGlobal(environmentPointer);
            if (capabilitiesPointer != IntPtr.Zero) Marshal.FreeHGlobal(capabilitiesPointer);
            if (attributeList != IntPtr.Zero) Marshal.FreeHGlobal(attributeList);
            if (sidPointer != IntPtr.Zero) Marshal.FreeHGlobal(sidPointer);
        }
    }

    private string BuildEnvironmentBlock()
    {
        var values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            values[(string)item.Key] = (string?)item.Value ?? "";
        // CreateProcess receives a custom environment block, so Windows cannot inject the
        // profile's virtualized values for these variables. Keep every writable runtime path
        // inside the per-task directory already granted to the AppContainer SID.
        values["TEMP"] = _taskDirectory;
        values["TMP"] = _taskDirectory;
        values["LOCALAPPDATA"] = _taskDirectory;
        values["DOTNET_CLI_HOME"] = Path.Combine(_taskDirectory, ".dotnet");
        values["NUGET_PACKAGES"] = Path.Combine(_taskDirectory, ".packages");
        values["DOTNET_NOLOGO"] = "1";
        values["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        return string.Join('\0', values.Select(item => item.Key + "=" + item.Value)) + "\0\0";
    }

    private static void GrantTaskDirectory(string taskDirectory, SecurityIdentifier sid)
    {
        var directory = new DirectoryInfo(taskDirectory);
        var security = directory.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None, AccessControlType.Allow));
        directory.SetAccessControl(security);
    }

    private static void ChangeWindowObjectAccess(IntPtr handle, SecurityIdentifier sid, uint access, bool grant)
    {
        IntPtr securityDescriptor = IntPtr.Zero, newAcl = IntPtr.Zero, sidPointer = IntPtr.Zero;
        try
        {
            var error = Native.GetSecurityInfo(handle, 7, DaclSecurityInformation,
                IntPtr.Zero, IntPtr.Zero, out var oldAcl, IntPtr.Zero, out securityDescriptor);
            if (error != 0) throw new SandboxException($"GetSecurityInfo falhou: erro {error}.");
            sidPointer = Marshal.AllocHGlobal(sid.BinaryLength);
            var bytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(bytes, 0);
            Marshal.Copy(bytes, 0, sidPointer, bytes.Length);
            var entry = new Native.ExplicitAccess
            {
                AccessPermissions = access,
                AccessMode = grant ? 1u : 4u,
                Inheritance = 0,
                Trustee = new Native.Trustee { TrusteeForm = 0, TrusteeType = 0, Name = sidPointer }
            };
            error = Native.SetEntriesInAcl(1, ref entry, oldAcl, out newAcl);
            if (error != 0) throw new SandboxException($"SetEntriesInAcl falhou: erro {error}.");
            error = Native.SetSecurityInfo(handle, 7, DaclSecurityInformation,
                IntPtr.Zero, IntPtr.Zero, newAcl, IntPtr.Zero);
            if (error != 0) throw new SandboxException($"SetSecurityInfo falhou: erro {error}.");
        }
        finally
        {
            if (newAcl != IntPtr.Zero) Native.LocalFree(newAcl);
            if (securityDescriptor != IntPtr.Zero) Native.LocalFree(securityDescriptor);
            if (sidPointer != IntPtr.Zero) Marshal.FreeHGlobal(sidPointer);
        }
    }

    private static string GetWindowObjectName(IntPtr handle)
    {
        Native.GetUserObjectInformation(handle, 2, null, 0, out var length);
        if (length == 0) ThrowLastError("GetUserObjectInformation(tamanho)");
        var name = new StringBuilder(checked((int)(length / sizeof(char))));
        if (!Native.GetUserObjectInformation(handle, 2, name, length, out _))
            ThrowLastError("GetUserObjectInformation(nome)");
        return name.ToString();
    }

    private static void ConfigureJob(IntPtr job, int memoryLimitMb, int processLimit)
    {
        var information = new Native.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new Native.JobObjectBasicLimitInformation
            {
                LimitFlags = JobObjectLimitKillOnJobClose | JobObjectLimitJobMemory | JobObjectLimitActiveProcess,
                ActiveProcessLimit = checked((uint)processLimit)
            },
            JobMemoryLimit = (nuint)checked((ulong)memoryLimitMb * 1024 * 1024)
        };
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Native.JobObjectExtendedLimitInformation>());
        try
        {
            Marshal.StructureToPtr(information, pointer, false);
            if (!Native.SetInformationJobObject(job, 9, pointer,
                    (uint)Marshal.SizeOf<Native.JobObjectExtendedLimitInformation>()))
                ThrowLastError("SetInformationJobObject");
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    private static ulong QueryPeakMemory(IntPtr job)
    {
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Native.JobObjectExtendedLimitInformation>());
        try
        {
            if (!Native.QueryInformationJobObject(job, 9, pointer,
                    (uint)Marshal.SizeOf<Native.JobObjectExtendedLimitInformation>(), IntPtr.Zero)) return 0;
            return (ulong)Marshal.PtrToStructure<Native.JobObjectExtendedLimitInformation>(pointer).PeakJobMemoryUsed;
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static void ThrowLastError(string operation) =>
        throw new SandboxException($"{operation} falhou: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (WindowObjectAclLock)
        {
            try { ChangeWindowObjectAccess(_desktop, _sid, 0, grant: false); } catch { }
            try { ChangeWindowObjectAccess(_windowStation, _sid, 0, grant: false); } catch { }
        }
        Native.CloseDesktop(_desktop);
        Native.CloseWindowStation(_windowStation);
        Native.DeleteAppContainerProfile(_profileName);
    }

    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct SecurityCapabilities
        { public IntPtr AppContainerSid; public IntPtr Capabilities; public uint CapabilityCount; public uint Reserved; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct StartupInfo
        {
            public int Cb; public string? Reserved; public string? Desktop; public string? Title;
            public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
            public short ShowWindow, Reserved2; public IntPtr ReservedPointer, StdInput, StdOutput, StdError;
        }
        [StructLayout(LayoutKind.Sequential)] public struct StartupInfoEx
        { public StartupInfo StartupInfo; public IntPtr AttributeList; }
        [StructLayout(LayoutKind.Sequential)] public struct ProcessInformation
        { public IntPtr ProcessHandle, ThreadHandle; public uint ProcessId, ThreadId; }
        [StructLayout(LayoutKind.Sequential)] public struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit, PerJobUserTimeLimit; public uint LimitFlags;
            public nuint MinimumWorkingSetSize, MaximumWorkingSetSize; public uint ActiveProcessLimit;
            public nuint Affinity; public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)] public struct IoCounters
        { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
        [StructLayout(LayoutKind.Sequential)] public struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation; public IoCounters IoInfo;
            public nuint ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
        }
        [StructLayout(LayoutKind.Sequential)] public struct Trustee
        {
            public IntPtr MultipleTrustee;
            public uint MultipleTrusteeOperation, TrusteeForm, TrusteeType;
            public IntPtr Name;
        }
        [StructLayout(LayoutKind.Sequential)] public struct ExplicitAccess
        {
            public uint AccessPermissions, AccessMode, Inheritance;
            public Trustee Trustee;
        }

        [DllImport("userenv.dll", CharSet = CharSet.Unicode)] public static extern int CreateAppContainerProfile(
            string name, string displayName, string description, IntPtr capabilities, uint capabilityCount, out IntPtr sid);
        [DllImport("userenv.dll", CharSet = CharSet.Unicode)] public static extern int DeleteAppContainerProfile(string name);
        [DllImport("advapi32.dll")] public static extern IntPtr FreeSid(IntPtr sid);
        [DllImport("advapi32.dll", SetLastError = true)] public static extern uint GetSecurityInfo(
            IntPtr handle, int objectType, uint securityInfo, IntPtr owner, IntPtr group,
            out IntPtr dacl, IntPtr sacl, out IntPtr securityDescriptor);
        [DllImport("advapi32.dll", SetLastError = true)] public static extern uint SetSecurityInfo(
            IntPtr handle, int objectType, uint securityInfo, IntPtr owner, IntPtr group,
            IntPtr dacl, IntPtr sacl);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern uint SetEntriesInAcl(
            uint count, ref ExplicitAccess entries, IntPtr oldAcl, out IntPtr newAcl);
        [DllImport("kernel32.dll")] public static extern IntPtr LocalFree(IntPtr memory);
        [DllImport("user32.dll")] public static extern IntPtr GetProcessWindowStation();
        [DllImport("user32.dll")] public static extern IntPtr GetThreadDesktop(uint threadId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool GetUserObjectInformation(
            IntPtr handle, int index, StringBuilder? information, uint length, out uint needed);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr OpenWindowStation(
            string name, bool inherit, uint desiredAccess);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr OpenDesktop(
            string name, uint flags, bool inherit, uint desiredAccess);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateDesktop(
            string name, string? device, IntPtr deviceMode, uint flags, uint desiredAccess, IntPtr securityAttributes);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool CloseWindowStation(IntPtr handle);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool CloseDesktop(IntPtr handle);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool InitializeProcThreadAttributeList(
            IntPtr list, int count, int flags, ref nuint size);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool UpdateProcThreadAttribute(
            IntPtr list, uint flags, IntPtr attribute, IntPtr value, nuint size, IntPtr previous, IntPtr returnSize);
        [DllImport("kernel32.dll")] public static extern void DeleteProcThreadAttributeList(IntPtr list);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool CreateProcess(
            string applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
            bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory,
            ref StartupInfoEx startupInfo, out ProcessInformation processInformation);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr CreateJobObject(IntPtr attributes, string? name);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool SetInformationJobObject(
            IntPtr job, int informationClass, IntPtr information, uint length);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool QueryInformationJobObject(
            IntPtr job, int informationClass, IntPtr information, uint length, IntPtr returnLength);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool TerminateJobObject(IntPtr job, uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);
    }
}
