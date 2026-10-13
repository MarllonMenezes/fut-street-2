using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglDeclarationElement(int TokenIndex, uint Token, int Stream, int Register,
    byte RawFormat, int ByteOffset, int ElementBytes);
public sealed record EaglDeclarationLayout(IReadOnlyList<EaglDeclarationElement> Elements, int ConsumedBytes);
public sealed record EaglMaterialDeclaration(EaglDeclarationLayout Layout, int Section, uint ParentOffset,
    uint DeclarationOffset, string DeclarationSha256, uint VertexProgramOffset, ushort VertexProgramVersion,
    ushort VertexInstructionCount, int VertexProgramBytes, string VertexProgramSha256, string SourceSha256);

/// <summary>
/// Bounded data decoder for the observed STREAM/REGISTER subset. The retail
/// consumer's bit fields and element-size tables are checked independently.
/// Register numbers are not positions, UVs, weights or a shader translation.
/// </summary>
public static class EaglMaterialDeclarationAudit
{
    public const string Source = "data/initstaz.big!initstay.big!mat.o";
    public const string SourceSha256 = "75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430";
    public const string Parent = "__RM:::ParentRM_FifastCharacterSkin";
    public const string VertexProgram = "FifastCharacterSkin0__EAGLMicroCodeVS";
    public const string Status = "verified-bounded-material-declaration-and-vs-envelope";

    internal static EaglDeclarationLayout Decode(ReadOnlySpan<byte> data)
    {
        var elements = new List<EaglDeclarationElement>();
        var registers = new HashSet<int>();
        var stream = -1;
        var offset = 0;
        for (var i = 0; i < Math.Min(data.Length / 4, 256); i++)
        {
            var token = BinaryPrimitives.ReadUInt32LittleEndian(data[(i * 4)..]);
            if (token == uint.MaxValue)
            {
                if (elements.Count == 0) throw new InvalidDataException("Empty declaration.");
                return new(elements, (i + 1) * 4);
            }
            if ((token & ~0xFu) == 0x20000000)
            {
                stream = (int)(token & 0xF);
                offset = 0;
                continue;
            }
            if ((token & ~0x00FF001Fu) != 0x40000000 || stream < 0)
                throw new InvalidDataException("Unsupported declaration token or missing stream.");
            var register = (int)(token & 0x1F);
            var format = (byte)(token >> 16);
            // Only formats observed in this source. No extrapolation to skip,
            // tessellation, constants, packed types or undocumented conversions.
            var bytes = format switch { 0x32 => 12, 0x22 => 8, 0x40 => 4, 0x15 => 2, _ => 0 };
            if (register > 15 || !registers.Add(register) || bytes == 0)
                throw new InvalidDataException("Unsupported format/register or duplicate register.");
            elements.Add(new(i, token, stream, register, format, offset, bytes));
            offset = checked(offset + bytes);
        }
        throw new InvalidDataException("Declaration terminator absent within bounded range.");
    }

    internal static EaglMaterialDeclaration Inspect(byte[] data, ElfInspection elf)
        => InspectNamed(data, elf, Parent, VertexProgram);

    internal static EaglMaterialDeclaration InspectNamed(byte[] data, ElfInspection elf, string parentName, string programName)
    {
        ElfSymbol Symbol(string name)
        {
            var values = elf.Symbols.Where(s => s.Name == name).ToArray();
            if (values.Length != 1 || values[0].SectionIndex == 0 || values[0].SectionIndex >= elf.Sections.Count)
                throw new InvalidDataException("Material symbol missing, duplicate or undefined.");
            return values[0];
        }
        var parent = Symbol(parentName);
        var program = Symbol(programName);
        var section = parent.SectionIndex;
        ReadOnlySpan<byte> Slice(uint offset, int count)
        {
            var s = elf.Sections[section];
            if (s.Type == 8 || count < 0 || (ulong)offset + (uint)count > s.Size ||
                (ulong)s.Offset + offset + (uint)count > (ulong)data.Length)
                throw new InvalidDataException("Material payload outside file-backed section.");
            return data.AsSpan(checked((int)((ulong)s.Offset + offset)), count);
        }
        uint LocalPointer(uint offset)
        {
            var refs = elf.Relocations.Where(r => r.TargetSectionIndex == section && r.Offset == offset).ToArray();
            if (refs.Length != 1 || refs[0].Type != 2 || refs[0].SymbolName.Length != 0)
                throw new InvalidDataException("Material reference is not a unique local pointer.");
            return BinaryPrimitives.ReadUInt32LittleEndian(Slice(offset, 4));
        }
        void NoRelocations(uint offset, int count)
        {
            if (elf.Relocations.Any(r => r.TargetSectionIndex == section && (ulong)r.Offset < (ulong)offset + (uint)count &&
                (ulong)r.Offset + 4 > offset)) throw new InvalidDataException("Opaque relocation within decoded payload.");
        }
        // The observed EAGL container carries e_machine=8. This is metadata,
        // not an executable CPU target or an instruction translator.
        if (elf.Type != 1 || elf.Machine != 8 || (parent.Value & 3) != 0 || (program.Value & 3) != 0)
            throw new InvalidDataException("Unsupported material object or symbol alignment.");
        Slice(parent.Value, 16); // Validate the whole prefix before uint field-offset arithmetic.
        NoRelocations(parent.Value + 4, 4);
        if (program.SectionIndex != section || BinaryPrimitives.ReadUInt32LittleEndian(Slice(parent.Value + 4, 4)) != 1)
            throw new InvalidDataException("Only a single-program parent in one section is supported.");
        var declaration = LocalPointer(parent.Value + 8);
        if ((declaration & 3) != 0) throw new InvalidDataException("Unaligned declaration.");
        var s = elf.Sections[section];
        if (declaration > s.Size) throw new InvalidDataException("Declaration outside section.");
        var layout = Decode(Slice(declaration, (int)Math.Min(1024u, s.Size - declaration)));
        NoRelocations(declaration, layout.ConsumedBytes);
        var programTable = LocalPointer(parent.Value + 12);
        var refs = elf.Relocations.Where(r => r.TargetSectionIndex == section && r.Offset == programTable).ToArray();
        if (refs.Length != 1 || refs[0].Type != 2 || refs[0].SymbolName != programName ||
            BinaryPrimitives.ReadUInt32LittleEndian(Slice(programTable, 4)) != 0)
            throw new InvalidDataException("Vertex program table does not refer to the exact named symbol.");
        var header = Slice(program.Value, 4);
        var version = BinaryPrimitives.ReadUInt16LittleEndian(header);
        var count = BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);
        if (version != 0x2078 || count is 0 or > 256)
            throw new InvalidDataException("Unsupported vertex program envelope.");
        var programBytes = checked(4 + count * 16);
        var payload = Slice(program.Value, programBytes);
        NoRelocations(program.Value, programBytes);
        return new(layout, section, parent.Value, declaration,
            Convert.ToHexString(SHA256.HashData(Slice(declaration, layout.ConsumedBytes))), program.Value, version, count,
            programBytes, Convert.ToHexString(SHA256.HashData(payload)), Convert.ToHexString(SHA256.HashData(data)));
    }

    internal static byte[] LoadSource(string image)
    {
        using var store = new GameAssetStore(image);
        using var nested = store.OpenBigEntry("data/initstaz.big", "initstay.big");
        if (nested.Length > 64 * 1024 * 1024) throw new InvalidDataException("Nested material source exceeds limit.");
        var compressed = new byte[checked((int)nested.Length)];
        nested.ReadExactly(compressed);
        byte[] archive;
        if (compressed.AsSpan().StartsWith("BIG4"u8) || compressed.AsSpan().StartsWith("BIGF"u8)) archive = compressed;
        else
        {
            var declared = RefPackDecoder.ReadDeclaredSize(compressed);
            if (declared > 64u * 1024u * 1024u) throw new InvalidDataException("Expanded material source exceeds limit.");
            using var memory = new MemoryStream(compressed, writable: false);
            archive = RefPackDecoder.Decode(memory, declared).Data;
        }
        var data = BigArchiveReader.ReadEntry(archive, "mat.o");
        if (data.Length != 38970 || Convert.ToHexString(SHA256.HashData(data)) != SourceSha256)
            throw new InvalidDataException("Material source identity changed.");
        return data;
    }

    public static int Run(string image, string report, string summary)
    {
        var paths = new[] { image, report, summary }.Select(Path.GetFullPath).ToArray();
        if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new ArgumentException("Image and output paths must differ.");
        var data = LoadSource(image);
        var parsed = Inspect(data, ElfReader.Inspect(data));
        Directory.CreateDirectory(Path.GetDirectoryName(paths[1])!);
        using (var writer = new StreamWriter(paths[1], false, new UTF8Encoding(false)))
        {
            writer.WriteLine("source_path\tsource_sha256\tparent_symbol\tsection\tparent_offset\tdeclaration_offset\ttoken_index\ttoken\tstream\tregister\traw_format\tbyte_offset\telement_bytes\tdeclaration_bytes\tdeclaration_sha256\tvertex_program_offset\tvertex_program_version\tvertex_instruction_count\tvertex_program_bytes\tvertex_program_sha256\tstatus\tattribute_semantics\tparent_inheritance_binding\toriginal_semantics");
            foreach (var e in parsed.Layout.Elements)
                writer.WriteLine(string.Join("\t", new object[] { Source, parsed.SourceSha256, Parent, parsed.Section,
                    Hex(parsed.ParentOffset), Hex(parsed.DeclarationOffset), e.TokenIndex, Hex(e.Token), e.Stream, e.Register,
                    Hex(e.RawFormat), e.ByteOffset, e.ElementBytes, parsed.Layout.ConsumedBytes, parsed.DeclarationSha256,
                    Hex(parsed.VertexProgramOffset), Hex(parsed.VertexProgramVersion), parsed.VertexInstructionCount,
                    parsed.VertexProgramBytes, parsed.VertexProgramSha256, Status, "not-proven", "not-proven", "not-claimed" }
                    .Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
        }
        var values = new Dictionary<string, string> {
            ["source_path"] = Source, ["source_sha256"] = SourceSha256,
            ["declaration_elements"] = parsed.Layout.Elements.Count.ToString(),
            ["declaration_bytes"] = parsed.Layout.ConsumedBytes.ToString(),
            ["vertex_instruction_count"] = parsed.VertexInstructionCount.ToString(),
            ["vertex_program_bytes"] = parsed.VertexProgramBytes.ToString(),
            ["report_sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(report))),
            ["status"] = Status, ["attribute_semantics"] = "not-proven",
            ["parent_inheritance_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
        EaglRenderParameterAudit.WriteSummary(summary, values);
        foreach (var (key, value) in values) Console.WriteLine($"{key}: {value}");
        return 0;
    }

    private static string Hex(uint value) => $"0x{value:X8}";
}
