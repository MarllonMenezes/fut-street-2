namespace FifaPcCompat.Runtime;

public sealed record X86BootProbeResult(
    uint InitialEip,
    uint FinalEip,
    int Steps,
    int DirectCalls,
    int KernelCalls,
    string StopReason,
    IReadOnlyList<string> Trace);

public sealed class X86BootProbe
{
    private const uint CarryFlag = 1;
    private const uint ZeroFlag = 1 << 6;
    private const uint SignFlag = 1 << 7;
    private const uint OverflowFlag = 1 << 11;
    private const uint MemorySize = 0x01000000;

    private readonly byte[] memory = new byte[MemorySize];
    private readonly XboxKernelRuntime kernel;
    private readonly List<string> trace = [];
    private uint eax;
    private uint ebx;
    private uint ecx;
    private uint edx;
    private uint esi;
    private uint edi;
    private uint ebp;
    private uint esp;
    private uint eflags;
    private uint eip;
    private int directCalls;
    private int kernelCalls;

    public X86BootProbe(byte[] xbe, XbeInspection inspection, XboxKernelRuntime kernel)
    {
        this.kernel = kernel;
        Map(xbe, inspection);
        eip = inspection.EntryPoint;
        esp = 0x00F00000;
        ebp = esp;
    }

    public X86BootProbeResult Run(int maxSteps = 500)
    {
        var initialEip = eip;
        var reason = "step limit";
        var steps = 0;
        for (; steps < maxSteps; steps++)
        {
            if (!Step(ref reason))
            {
                break;
            }
        }
        return new X86BootProbeResult(initialEip, eip, steps, directCalls, kernelCalls, reason, trace.ToArray());
    }

    private bool Step(ref string reason)
    {
        if (eip == 0)
        {
            reason = "entry point returned";
            return false;
        }
        var start = eip;
        var opcode = Read8(eip);
        try
        {
            switch (opcode)
            {
                case 0x05:
                    Add(ref eax, Read32(eip + 1));
                    eip += 5;
                    break;
                case 0x0F:
                    return Stop(ref reason, start, "two-byte opcode");
                case 0x2B:
                    if (Read8(eip + 1) == 0xC1)
                    {
                        Sub(ref eax, ecx);
                        eip += 2;
                    }
                    else if (Read8(eip + 1) == 0x05)
                    {
                        var address = Read32(eip + 2);
                        Sub(ref eax, Read32(address));
                        eip += 6;
                    }
                    else if (Read8(eip + 1) == 0x50 && Read8(eip + 2) == 0x28)
                    {
                        Sub(ref edx, Read32(eax + 0x28));
                        eip += 3;
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported sub form");
                    }
                    break;
                case 0x31:
                    if (Read8(eip + 1) == 0xC0) eax = 0;
                    else if (Read8(eip + 1) == 0xF6) esi = 0;
                    else return Stop(ref reason, start, "unsupported xor form");
                    SetLogicFlags(0);
                    eip += 2;
                    break;
                case 0x33:
                    if (Read8(eip + 1) == 0xC0) eax = 0;
                    else if (Read8(eip + 1) == 0xC9) ecx = 0;
                    else if (Read8(eip + 1) == 0xF6) esi = 0;
                    else return Stop(ref reason, start, "unsupported xor form");
                    SetLogicFlags(0);
                    eip += 2;
                    break;
                case 0x3B:
                    if (Read8(eip + 1) == 0x01)
                    {
                        Compare(eax, Read32(ecx));
                    }
                    else if (Read8(eip + 1) == 0xFE)
                    {
                        Compare(edi, esi);
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported cmp form");
                    }
                    eip += 2;
                    break;
                case 0x40:
                    eax++;
                    eip++;
                    break;
                case 0x50 or 0x51 or 0x52 or 0x53 or 0x54 or 0x55 or 0x56 or 0x57:
                    Push(GetRegister(opcode - 0x50));
                    eip++;
                    break;
                case 0x58 or 0x59 or 0x5A or 0x5B or 0x5C or 0x5D or 0x5E or 0x5F:
                    SetRegister(opcode - 0x58, Pop());
                    eip++;
                    break;
                case 0x64:
                    return Stop(ref reason, start, "FS-segment access requires Xbox TLS");
                case 0x68:
                    Push(Read32(eip + 1));
                    eip += 5;
                    break;
                case 0x6A:
                    Push(unchecked((uint)(int)(sbyte)Read8(eip + 1)));
                    eip += 2;
                    break;
                case 0x73:
                    BranchShort((eflags & CarryFlag) == 0, Read8(eip + 1));
                    break;
                case 0x74:
                    BranchShort((eflags & ZeroFlag) != 0, Read8(eip + 1));
                    break;
                case 0x75:
                    BranchShort((eflags & ZeroFlag) == 0, Read8(eip + 1));
                    break;
                case 0x7C:
                    BranchShort(Sign() != Overflow(), Read8(eip + 1));
                    break;
                case 0x7D:
                    BranchShort(Sign() == Overflow(), Read8(eip + 1));
                    break;
                case 0x81:
                    if (Read8(eip + 1) == 0xC2)
                    {
                        Add(ref edx, Read32(eip + 2));
                        eip += 6;
                    }
                    else if (Read8(eip + 1) == 0xE1)
                    {
                        ecx &= Read32(eip + 2);
                        SetLogicFlags(ecx);
                        eip += 6;
                    }
                    else if (Read8(eip + 1) == 0x78 && Read8(eip + 2) == 0x08)
                    {
                        Compare(Read32(eax + 8), Read32(eip + 3));
                        eip += 7;
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported 81 form");
                    }
                    break;
                case 0x83:
                    if (Read8(eip + 1) == 0xC0)
                    {
                        Add(ref eax, unchecked((uint)(int)(sbyte)Read8(eip + 2)));
                        eip += 3;
                    }
                    else if (Read8(eip + 1) == 0xE0)
                    {
                        eax &= unchecked((uint)(int)(sbyte)Read8(eip + 2));
                        SetLogicFlags(eax);
                        eip += 3;
                    }
                    else if (Read8(eip + 1) == 0xEC)
                    {
                        esp -= Read8(eip + 2);
                        eip += 3;
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported 83 form");
                    }
                    break;
                case 0x85:
                    if (Read8(eip + 1) == 0xC0) Test(eax);
                    else if (Read8(eip + 1) == 0xC9) Test(ecx);
                    else return Stop(ref reason, start, "unsupported test form");
                    eip += 2;
                    break;
                case 0x89:
                    if (Read8(eip + 1) == 0x51 && Read8(eip + 2) == 0x04)
                    {
                        Write32(ecx + 4, edx);
                        eip += 3;
                    }
                    else if (Read8(eip + 1) == 0x01)
                    {
                        Write32(ecx, eax);
                        eip += 2;
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported 89 form");
                    }
                    break;
                case 0x8B:
                    if (!ExecuteMovOrLea(ref reason)) return false;
                    break;
                case 0x8D:
                    if (!ExecuteLea(ref reason)) return false;
                    break;
                case 0x99:
                    edx = (eax & 0x80000000) == 0 ? 0u : 0xFFFFFFFFu;
                    eip++;
                    break;
                case 0xA1:
                    eax = Read32(Read32(eip + 1));
                    eip += 5;
                    break;
                case 0xA3:
                    Write32(Read32(eip + 1), eax);
                    eip += 5;
                    break;
                case 0xB8 or 0xB9 or 0xBA or 0xBB or 0xBC or 0xBD or 0xBE or 0xBF:
                    SetRegister(opcode - 0xB8, Read32(eip + 1));
                    eip += 5;
                    break;
                case 0xC2:
                    var stackAdjust = Read16(eip + 1);
                    eip = Pop();
                    esp += stackAdjust;
                    break;
                case 0xC3:
                    eip = Pop();
                    break;
                case 0xC6:
                    if (Read8(eip + 1) != 0x01) return Stop(ref reason, start, "unsupported C6 form");
                    Write8(ecx, Read8(eip + 2));
                    eip += 3;
                    break;
                case 0xC1:
                    if (Read8(eip + 1) != 0xE9) return Stop(ref reason, start, "unsupported C1 form");
                    ecx >>= Read8(eip + 2);
                    SetLogicFlags(ecx);
                    eip += 3;
                    break;
                case 0xC9:
                    esp = ebp;
                    ebp = Pop();
                    eip++;
                    break;
                case 0xE8:
                    var target = unchecked((uint)(eip + 5 + (int)Read32(eip + 1)));
                    Push(eip + 5);
                    eip = target;
                    directCalls++;
                    break;
                case 0xE9:
                    eip = unchecked((uint)(eip + 5 + (int)Read32(eip + 1)));
                    break;
                case 0xEB:
                    BranchShort(true, Read8(eip + 1));
                    break;
                case 0xF7:
                    if (Read8(eip + 1) != 0xF9) return Stop(ref reason, start, "unsupported F7 form");
                    var dividend = ((long)(int)edx << 32) | eax;
                    var divisor = (int)ecx;
                    if (divisor == 0) return Stop(ref reason, start, "division by zero");
                    eax = unchecked((uint)(dividend / divisor));
                    edx = unchecked((uint)(dividend % divisor));
                    eip += 2;
                    break;
                case 0xFF:
                    if (Read8(eip + 1) == 0x15)
                    {
                        Push(eip + 6);
                        if (!ExecuteKernelCall(ref reason, start)) return false;
                    }
                    else if (Read8(eip + 1) == 0x35)
                    {
                        Push(Read32(Read32(eip + 2)));
                        eip += 6;
                    }
                    else if (Read8(eip + 1) == 0x75)
                    {
                        Push(Read32(ebp + Read8(eip + 2)));
                        eip += 3;
                    }
                    else if (Read8(eip + 1) == 0x74 && Read8(eip + 2) == 0x24 && Read8(eip + 3) == 0x04)
                    {
                        Push(Read32(esp + 4));
                        eip += 4;
                    }
                    else
                    {
                        return Stop(ref reason, start, "unsupported FF form");
                    }
                    break;
                default:
                    return Stop(ref reason, start, $"unsupported opcode 0x{opcode:X2}");
            }
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or InvalidDataException)
        {
            return Stop(ref reason, start, $"memory fault: {ex.Message}");
        }

        if (trace.Count < 64)
        {
            trace.Add($"0x{start:X8}: opcode 0x{opcode:X2}, next=0x{eip:X8}, esp=0x{esp:X8}");
        }
        return true;
    }

    private bool ExecuteMovOrLea(ref string reason)
    {
        var modrm = Read8(eip + 1);
        switch (modrm)
        {
            case 0x0D:
                ecx = Read32(Read32(eip + 2));
                eip += 6;
                return true;
            case 0x15:
                edx = Read32(Read32(eip + 2));
                eip += 6;
                return true;
            case 0x45:
                eax = Read32(ebp + Read8(eip + 2));
                eip += 3;
                return true;
            case 0x4D:
                ecx = Read32(ebp + Read8(eip + 2));
                eip += 3;
                return true;
            case 0x80:
                eax = Read32(eax + Read32(eip + 2));
                eip += 6;
                return true;
            case 0x14 when Read8(eip + 2) == 0x97:
                edx = Read32(edi + edx * 4);
                eip += 3;
                return true;
            case 0xEC:
                ebp = esp;
                eip += 2;
                return true;
            case 0xC8:
                ecx = eax;
                eip += 2;
                return true;
            case 0xF8:
                edi = eax;
                eip += 2;
                return true;
            default:
                Stop(ref reason, eip, $"unsupported 8B form 0x{modrm:X2}");
                return false;
        }
    }

    private bool ExecuteLea(ref string reason)
    {
        if (Read8(eip + 1) == 0x44 && Read8(eip + 2) == 0x08)
        {
            eax = eax + ecx + Read8(eip + 3);
            eip += 4;
            return true;
        }
        if (Read8(eip + 1) == 0x45)
        {
            eax = ebp + Read8(eip + 2);
            eip += 3;
            return true;
        }
        if (Read8(eip + 1) == 0x85)
        {
            eax = unchecked(ebp + Read32(eip + 2));
            eip += 6;
            return true;
        }
        Stop(ref reason, eip, "unsupported 8D form");
        return false;
    }

    private bool ExecuteKernelCall(ref string reason, uint start)
    {
        var thunk = Read32(Read32(eip + 2));
        if ((thunk & 0x80000000) == 0)
        {
            return Stop(ref reason, start, $"non-kernel indirect call 0x{thunk:X8}");
        }

        var ordinal = thunk & 0x7FFFFFFF;
        kernelCalls++;
        if (ordinal == (uint)XboxKernelImport.PsCreateSystemThreadEx)
        {
            var arguments = new nint[XboxPsCreateSystemThreadExCall.ObservedArgumentCount];
            for (var index = 0; index < arguments.Length; index++)
            {
                arguments[index] = (nint)(int)Read32(esp + 4 + (uint)(index * 4));
            }
            var call = XboxPsCreateSystemThreadExCall.FromObservedStartup(arguments);
            kernel.TryRecordPsCreateSystemThreadEx(call, out var recordHandle);
            if (call.Argument0 != 0)
            {
                Write32((uint)call.Argument0, unchecked((uint)recordHandle));
            }
            var returnAddress = Read32(esp);
            esp += 4 + (uint)(arguments.Length * 4);
            eax = 0;
            eip = returnAddress;
            return true;
        }

        if (ordinal == (uint)XboxKernelImport.NtClose)
        {
            var handle = (nint)(int)Read32(esp + 4);
            kernel.TryCloseHandle(handle, out var status);
            var returnAddress = Read32(esp);
            esp += 8;
            eax = unchecked((uint)status);
            eip = returnAddress;
            return true;
        }

        return Stop(ref reason, start, $"unimplemented XBOXKRNL ordinal {ordinal}");
    }

    private bool Stop(ref string reason, uint address, string message)
    {
        reason = $"0x{address:X8}: {message}";
        return false;
    }

    private void BranchShort(bool condition, byte displacement)
    {
        eip += 2;
        if (condition)
        {
            eip = unchecked((uint)(eip + (sbyte)displacement));
        }
    }

    private void Push(uint value)
    {
        esp -= 4;
        Write32(esp, value);
    }

    private uint Pop()
    {
        var value = Read32(esp);
        esp += 4;
        return value;
    }

    private uint GetRegister(int index) => index switch
    {
        0 => eax,
        1 => ecx,
        2 => edx,
        3 => ebx,
        4 => esp,
        5 => ebp,
        6 => esi,
        7 => edi,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    private void SetRegister(int index, uint value)
    {
        switch (index)
        {
            case 0: eax = value; break;
            case 1: ecx = value; break;
            case 2: edx = value; break;
            case 3: ebx = value; break;
            case 4: esp = value; break;
            case 5: ebp = value; break;
            case 6: esi = value; break;
            case 7: edi = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void Add(ref uint destination, uint value)
    {
        var left = destination;
        destination += value;
        SetAddFlags(left, value, destination);
    }

    private void Sub(ref uint destination, uint value)
    {
        var left = destination;
        destination -= value;
        SetSubFlags(left, value, destination);
    }

    private void Compare(uint left, uint right)
    {
        var result = left - right;
        SetSubFlags(left, right, result);
    }

    private void Test(uint value) => SetLogicFlags(value);
    private bool Sign() => (eflags & SignFlag) != 0;
    private bool Overflow() => (eflags & OverflowFlag) != 0;

    private void SetLogicFlags(uint value)
    {
        eflags &= ~(CarryFlag | ZeroFlag | SignFlag | OverflowFlag);
        if (value == 0) eflags |= ZeroFlag;
        if ((value & 0x80000000) != 0) eflags |= SignFlag;
    }

    private void SetAddFlags(uint left, uint right, uint result)
    {
        eflags &= ~(CarryFlag | ZeroFlag | SignFlag | OverflowFlag);
        if ((ulong)left + right > uint.MaxValue) eflags |= CarryFlag;
        if (result == 0) eflags |= ZeroFlag;
        if ((result & 0x80000000) != 0) eflags |= SignFlag;
        if (((left ^ result) & (right ^ result) & 0x80000000) != 0) eflags |= OverflowFlag;
    }

    private void SetSubFlags(uint left, uint right, uint result)
    {
        eflags &= ~(CarryFlag | ZeroFlag | SignFlag | OverflowFlag);
        if (left < right) eflags |= CarryFlag;
        if (result == 0) eflags |= ZeroFlag;
        if ((result & 0x80000000) != 0) eflags |= SignFlag;
        if (((left ^ right) & (left ^ result) & 0x80000000) != 0) eflags |= OverflowFlag;
    }

    private void Map(byte[] xbe, XbeInspection inspection)
    {
        WriteBytes(inspection.BaseAddress, xbe);
        foreach (var section in inspection.Sections)
        {
            WriteBytes(section.VirtualAddress, xbe.AsSpan(checked((int)section.FileAddress), checked((int)section.FileSize)).ToArray());
        }
    }

    private byte Read8(uint address) => memory[checked((int)address)];
    private ushort Read16(uint address) => BitConverter.ToUInt16(memory, checked((int)address));
    private uint Read32(uint address) => BitConverter.ToUInt32(memory, checked((int)address));
    private void Write8(uint address, byte value) => memory[checked((int)address)] = value;
    private void Write32(uint address, uint value) => BitConverter.GetBytes(value).CopyTo(memory, checked((int)address));
    private void WriteBytes(uint address, byte[] data) => data.CopyTo(memory, checked((int)address));
}
