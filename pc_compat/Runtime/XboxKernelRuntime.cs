namespace FifaPcCompat.Runtime;

public static class XboxNtStatus
{
    public const int Success = 0;
    public const int InvalidHandle = unchecked((int)0xC0000008);
    public const int NotImplemented = unchecked((int)0xC0000002);
}

public sealed record XboxThreadRecord(
    nint Handle,
    nint StartRoutine,
    nint Context,
    Task? ManagedTask = null,
    XboxPsCreateSystemThreadExCall? RawCreateCall = null);

public sealed class XboxKernelRuntime : IXboxKernel
{
    private readonly object gate = new();
    private readonly Dictionary<nint, XboxThreadRecord> threadHandles = [];
    private readonly Dictionary<nint, Action<nint>> managedRoutines = [];
    private long nextHandle = 0x1000;

    public bool ExecutionEnabled => false;
    public int OpenHandleCount
    {
        get
        {
            lock (gate)
            {
                return threadHandles.Count;
            }
        }
    }

    public bool TryCloseHandle(nint handle, out int status)
    {
        lock (gate)
        {
            if (!threadHandles.Remove(handle))
            {
                status = XboxNtStatus.InvalidHandle;
                return false;
            }
        }

        status = XboxNtStatus.Success;
        return true;
    }

    public bool TryCreateSystemThreadEx(nint startRoutine, nint context, out nint threadHandle)
    {
        if (startRoutine == 0)
        {
            threadHandle = 0;
            return false;
        }

        lock (gate)
        {
            threadHandle = (nint)nextHandle;
            nextHandle += 4;
            threadHandles.Add(threadHandle, new XboxThreadRecord(threadHandle, startRoutine, context));
        }

        // The pointer is an Xbox virtual address. It is recorded for later ABI
        // work; it is intentionally not cast to a managed delegate or invoked.
        return true;
    }

    public void RegisterManagedRoutine(nint xboxAddress, Action<nint> routine)
    {
        ArgumentNullException.ThrowIfNull(routine);
        lock (gate)
        {
            managedRoutines[xboxAddress] = routine;
        }
    }

    public bool TryStartRegisteredRoutine(nint startRoutine, nint context, out nint threadHandle, out int status)
    {
        Action<nint>? routine;
        lock (gate)
        {
            managedRoutines.TryGetValue(startRoutine, out routine);
            if (routine is null)
            {
                threadHandle = 0;
                status = XboxNtStatus.NotImplemented;
                return false;
            }

            threadHandle = (nint)nextHandle;
            nextHandle += 4;
        }

        var task = Task.Run(() => routine(context));
        lock (gate)
        {
            threadHandles.Add(threadHandle, new XboxThreadRecord(threadHandle, startRoutine, context, task));
        }
        status = XboxNtStatus.Success;
        return true;
    }

    public bool TryRecordPsCreateSystemThreadEx(XboxPsCreateSystemThreadExCall call, out nint recordHandle)
    {
        // The actual Xbox ABI remains positional until more call sites confirm
        // its names. Recording preserves every argument without invoking an address.
        lock (gate)
        {
            recordHandle = (nint)nextHandle;
            nextHandle += 4;
            threadHandles.Add(recordHandle, new XboxThreadRecord(recordHandle, 0, 0, null, call));
        }
        return true;
    }

    public IReadOnlyList<XboxThreadRecord> SnapshotThreads()
    {
        lock (gate)
        {
            return threadHandles.Values.ToArray();
        }
    }
}
