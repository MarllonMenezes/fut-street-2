namespace FifaPcCompat.Runtime;

public enum XboxKernelImport : uint
{
    NtClose = 187,
    PsCreateSystemThreadEx = 255,
}

public interface IXboxKernel
{
    // Contracts are named from the verified XBOXKRNL ordinals. The host
    // implementation will be added only after call-site ABI validation.
    bool TryCloseHandle(nint handle, out int status);
    bool TryCreateSystemThreadEx(nint startRoutine, nint context, out nint threadHandle);
}

public interface IXboxFileSystem
{
    Stream OpenReadOnly(string virtualPath);
    IEnumerable<string> Enumerate(string virtualDirectory);
}

public interface IGraphicsBackend
{
    string BackendName { get; }
    void Initialize(int width, int height);
}

public interface IAudioBackend
{
    string BackendName { get; }
    void Initialize();
}

public sealed class CompatibilityHost
{
    private readonly XboxKernelRuntime kernel = new();

    public string Status()
    {
        return string.Join(Environment.NewLine,
            "FIFA Street 2 PC compatibility host",
            "phase: contract-only",
            "xbe execution: not enabled",
            "original ISO: read-only",
            $"verified kernel imports: {string.Join(", ", Enum.GetValues<XboxKernelImport>().Select(value => $"{value}={(uint)value}"))}",
            $"kernel mode: {(kernel.ExecutionEnabled ? "execute" : "record-only")}",
            "graphics backend: not selected",
            "audio backend: not selected");
    }

    public void ValidateContracts()
    {
        var values = Enum.GetValues<XboxKernelImport>().Select(value => (uint)value).ToHashSet();
        if (!values.SetEquals([187u, 255u]))
        {
            throw new InvalidOperationException("Verified XBOXKRNL ordinal set changed unexpectedly.");
        }
    }

    public string InspectImage(string imagePath)
    {
        using var reader = new XdvdfsReader(imagePath);
        var entries = reader.Entries();
        var xbe = XbeInspector.Inspect(reader.ReadFile("default.xbe"));
        var allHashesMatch = xbe.Sections.All(section => section.HashMatches);
        return string.Join(Environment.NewLine,
            $"image: {imagePath}",
            $"partition_offset: 0x{reader.PartitionOffset:X}",
            $"entries: {entries.Count}",
            $"xbe_base: 0x{xbe.BaseAddress:X8}",
            $"xbe_entry_point: 0x{xbe.EntryPoint:X8}",
            $"xbe_sections: {xbe.SectionCount}",
            $"xbe_section_hashes: {(allHashesMatch ? "OK" : "MISMATCH")}",
            $"xboxkrnl_thunks: {xbe.KernelThunkCount}",
            "execution: disabled");
    }

    public string FileSystemSmoke(string imagePath, string virtualPath)
    {
        using var fileSystem = new XdvdfsFileSystem(imagePath);
        using var stream = fileSystem.OpenReadOnly(virtualPath);
        var header = new byte[checked((int)Math.Min(4L, stream.Length))];
        _ = stream.Read(header, 0, header.Length);
        var directoryCount = fileSystem.Enumerate("data").Count();
        return string.Join(Environment.NewLine,
            $"virtual_path: {virtualPath}",
            $"size: {stream.Length}",
            $"first_bytes: {Convert.ToHexString(header)}",
            $"data_entries: {directoryCount}",
            "mode: read-only");
    }

    public string BigArchiveSmoke(string imagePath, string virtualPath)
    {
        using var fileSystem = new XdvdfsFileSystem(imagePath);
        var archive = BigArchiveReader.Inspect(fileSystem, virtualPath);
        var first = archive.Entries.FirstOrDefault();
        return string.Join(Environment.NewLine,
            $"virtual_path: {virtualPath}",
            $"magic: {archive.Magic}",
            $"archive_size: {archive.ArchiveSize}",
            $"entries: {archive.FileCount}",
            $"index_end: {archive.IndexEnd}",
            $"archive_size_matches: {archive.ArchiveSizeMatches}",
            $"first_entry: {first?.Name ?? "<none>"}",
            "mode: read-only");
    }

    public string KernelSmoke()
    {
        var created = kernel.TryCreateSystemThreadEx((nint)0x0024EAA2, (nint)0x1234, out var handle);
        var closeFirst = kernel.TryCloseHandle(handle, out var closeStatus);
        var closeSecond = kernel.TryCloseHandle(handle, out var secondStatus);
        return string.Join(Environment.NewLine,
            $"PsCreateSystemThreadEx record: {created}",
            $"handle: 0x{handle.ToInt64():X}",
            $"NtClose first: {closeFirst}, status=0x{closeStatus:X8}",
            $"NtClose second: {closeSecond}, status=0x{secondStatus:X8}",
            $"open_handles: {kernel.OpenHandleCount}",
            "execution: disabled");
    }

    public string KernelAbiSmoke()
    {
        var call = XboxPsCreateSystemThreadExCall.FromObservedStartup(
            (nint)0x00200000, 0, 0, 0, 0, 0, 0, 0, 0, (nint)0x0024EAA2);
        var recorded = kernel.TryRecordPsCreateSystemThreadEx(call, out var handle);
        var closed = kernel.TryCloseHandle(handle, out var status);
        return string.Join(Environment.NewLine,
            $"raw_arguments: {call.Arguments.Count}",
            $"observed_trailing_constant: 0x{call.Argument9.ToInt64():X8}",
            $"recorded: {recorded}",
            $"record_handle: 0x{handle.ToInt64():X}",
            $"NtClose: {closed}, status=0x{status:X8}",
            "argument_names: unresolved",
            "execution: disabled");
    }

    public string KernelSchedulerSmoke()
    {
        using var callbackRan = new ManualResetEventSlim(false);
        const nint xboxRoutine = (nint)0x0024EAA2;
        kernel.RegisterManagedRoutine(xboxRoutine, _ => callbackRan.Set());
        var started = kernel.TryStartRegisteredRoutine(xboxRoutine, (nint)0x1234, out var handle, out var startStatus);
        var ran = callbackRan.Wait(TimeSpan.FromSeconds(2));
        var closeStatus = XboxNtStatus.InvalidHandle;
        var closed = started && kernel.TryCloseHandle(handle, out closeStatus);
        return string.Join(Environment.NewLine,
            $"registered_routine: 0x{xboxRoutine.ToInt64():X8}",
            $"started: {started}, status=0x{startStatus:X8}",
            $"managed_callback_ran: {ran}",
            $"NtClose: {closed}, status=0x{(started ? closeStatus : XboxNtStatus.InvalidHandle):X8}",
            "xbox_pointer_execution: disabled");
    }

    public string CpuProbe(string imagePath, int maxSteps)
    {
        using var reader = new XdvdfsReader(imagePath);
        var xbeData = reader.ReadFile("default.xbe");
        var inspection = XbeInspector.Inspect(xbeData);
        var probe = new X86BootProbe(xbeData, inspection, kernel);
        var result = probe.Run(Math.Max(1, maxSteps));
        return string.Join(Environment.NewLine,
            $"initial_eip: 0x{result.InitialEip:X8}",
            $"final_eip: 0x{result.FinalEip:X8}",
            $"steps: {result.Steps}",
            $"direct_calls: {result.DirectCalls}",
            $"kernel_calls: {result.KernelCalls}",
            $"stop: {result.StopReason}",
            "execution_mode: managed_probe_only",
            "original_xbe_pointer_execution: disabled",
            "trace:",
            string.Join(Environment.NewLine, result.Trace));
    }
}
