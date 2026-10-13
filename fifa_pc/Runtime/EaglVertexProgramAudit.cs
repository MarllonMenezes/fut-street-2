using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglVertexSource(uint Raw, int Mux, int Temporary, string Swizzle, bool Negate);
public sealed record EaglVertexInstruction(int Index, uint Word0, uint Word1, uint Word2, uint Word3,
    int Mac, int Ilu, int Input, int Constant, bool Relative, bool Final,
    EaglVertexSource A, EaglVertexSource B, EaglVertexSource C, int MacSources,
    int MacMask, int IluMask, int Temporary, int OutputMask, int OutputBank, int OutputAddress, bool OutputScalar)
{
    // Opcode-active source slots, not component liveness or a transitive dataflow proof.
    public IEnumerable<EaglVertexSource> ActiveSources => new[] { A, B, C }
        .Where((_, i) => ((MacSources | (Ilu == 0 ? 0 : 4)) & (1 << i)) != 0);
}

/// <summary>
/// Static bounded instruction-data inspection. No floating-point evaluation,
/// shader interpreter, CPU translation, GPU emulation or runtime draw binding.
/// Field positions: public Kelvin ISA plus NV2A DWORD storage documentation.
/// </summary>
public static class EaglVertexProgramAudit
{
    public const string Status = "verified-bounded-vertex-program-opcode-operands";
    public const string ProgramSha256 = "B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67";
    internal static readonly string[] MacNames = { "NOP", "MOV", "MUL", "ADD", "MAD", "DP3", "DPH", "DP4", "DST", "MIN", "MAX", "SLT", "SGE", "ARL" };
    internal static readonly string[] IluNames = { "NOP", "MOV", "RCP", "RCC", "RSQ", "EXP", "LOG", "LIT" };

    internal static IReadOnlyList<EaglVertexInstruction> Decode(ReadOnlySpan<byte> program)
    {
        if (program.Length < 4 || BinaryPrimitives.ReadUInt16LittleEndian(program) != 0x2078)
            throw new InvalidDataException("Unsupported vertex envelope.");
        var count = BinaryPrimitives.ReadUInt16LittleEndian(program[2..]);
        if (count is 0 or > 136 || program.Length != 4 + 16 * count)
            throw new InvalidDataException("Vertex program length/count outside bounded NV2A subset.");
        var result = new List<EaglVertexInstruction>();
        for (var i = 0; i < count; i++)
        {
            var bytes = program.Slice(4 + 16 * i, 16);
            var w0 = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            var w1 = BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]);
            var w2 = BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]);
            var w3 = BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]);
            if (w0 != 0 || (w1 & 0xF0000000) != 0)
                throw new InvalidDataException("Unsupported reserved instruction bits.");
            var mac = (int)((w1 >> 21) & 15);
            var ilu = (int)((w1 >> 25) & 7);
            if (mac >= MacNames.Length) throw new InvalidDataException("Unsupported vector opcode.");
            var sources = mac switch { 0 => 0, 1 or 13 => 1, 3 => 5, 4 => 7, _ => 3 };
            EaglVertexSource Source(uint raw)
            {
                var swizzle = (raw >> 6) & 255;
                return new(raw, (int)(raw & 3), (int)((raw >> 2) & 15),
                    string.Concat(Enumerable.Range(0, 4).Select(c => "xyzw"[(int)((swizzle >> (6 - c * 2)) & 3)])),
                    (raw & 0x4000) != 0);
            }
            var instruction = new EaglVertexInstruction(i, w0, w1, w2, w3, mac, ilu,
                (int)((w1 >> 9) & 15), (int)((w1 >> 13) & 255), (w3 & 2) != 0, (w3 & 1) != 0,
                Source(((w1 & 511) << 6) | (w2 >> 26)), Source((w2 >> 11) & 32767),
                Source(((w2 & 2047) << 4) | (w3 >> 28)), sources,
                (int)((w3 >> 24) & 15), (int)((w3 >> 16) & 15), (int)((w3 >> 20) & 15),
                (int)((w3 >> 12) & 15), (int)((w3 >> 11) & 1), (int)((w3 >> 3) & 255), (w3 & 4) != 0);
            if (instruction.Final != (i == count - 1))
                throw new InvalidDataException("Final marker does not match the envelope's last instruction.");
            foreach (var src in instruction.ActiveSources)
                if (src.Mux == 0 || (src.Mux == 1 && src.Temporary > 12) ||
                    (src.Mux == 3 && instruction.Constant > 191))
                    throw new InvalidDataException("Unsupported active source register/mux.");
            if ((instruction.MacMask != 0 && mac == 0) || (instruction.IluMask != 0 && ilu == 0) ||
                ((instruction.MacMask | instruction.IluMask) != 0 && instruction.Temporary > 11))
                throw new InvalidDataException("Unsupported temporary write fields.");
            if (instruction.OutputMask != 0 &&
                ((instruction.OutputScalar ? ilu : mac) == 0 ||
                 (instruction.OutputBank == 1 && instruction.OutputAddress is not (0 or >= 3 and <= 12)) ||
                 (instruction.OutputBank == 0 && instruction.OutputAddress > 191)))
                throw new InvalidDataException("Unsupported active output fields.");
            result.Add(instruction);
        }
        return result;
    }

    internal static Dictionary<string, string> Row(EaglVertexInstruction i, string programHash)
    {
        string Ref(EaglVertexSource s) => (s.Negate ? "-" : "") + (s.Mux switch {
            1 => $"r[{s.Temporary}]", 2 => $"v[{i.Input}]",
            3 => i.Relative ? $"c[a0.x+{i.Constant}]" : $"c[{i.Constant}]", _ => "unsupported" }) + "." + s.Swizzle;
        string Slots(int mask) => string.Concat(Enumerable.Range(0, 3).Where(n => (mask & (1 << n)) != 0).Select(n => "ABC"[n]));
        var active = i.MacSources | (i.Ilu == 0 ? 0 : 4);
        return new() {
            ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
            ["vertex_program_sha256"] = programHash, ["instruction_index"] = Num(i.Index),
            ["word0"] = Hex(i.Word0), ["word1"] = Hex(i.Word1), ["word2"] = Hex(i.Word2), ["word3"] = Hex(i.Word3),
            ["mac_opcode"] = Num(i.Mac), ["mac_name"] = MacNames[i.Mac], ["ilu_opcode"] = Num(i.Ilu), ["ilu_name"] = IluNames[i.Ilu],
            ["mac_source_slots"] = Slots(i.MacSources), ["ilu_source_slots"] = i.Ilu == 0 ? "" : "C",
            ["input_index_field"] = Num(i.Input), ["constant_index_field"] = Num(i.Constant), ["relative_constant"] = Flag(i.Relative),
            ["a_raw"] = Hex(i.A.Raw), ["b_raw"] = Hex(i.B.Raw), ["c_raw"] = Hex(i.C.Raw),
            ["a_operand"] = (active & 1) == 0 ? "inactive" : Ref(i.A),
            ["b_operand"] = (active & 2) == 0 ? "inactive" : Ref(i.B),
            ["c_operand"] = (active & 4) == 0 ? "inactive" : Ref(i.C),
            ["active_input_slots"] = Slots(Enumerable.Range(0, 3).Where(n => (active & (1 << n)) != 0 && new[] { i.A, i.B, i.C }[n].Mux == 2).Sum(n => 1 << n)),
            ["mac_temporary_mask"] = Num(i.MacMask), ["ilu_temporary_mask"] = Num(i.IluMask), ["raw_shared_temporary"] = Num(i.Temporary),
            ["output_mask"] = Num(i.OutputMask), ["output_bank"] = Num(i.OutputBank), ["output_address"] = Num(i.OutputAddress),
            ["output_unit"] = i.OutputScalar ? "ILU" : "MAC", ["final"] = Flag(i.Final),
            ["status"] = Status, ["operand_scope"] = "opcode-active-source-slots-not-component-liveness",
            ["parent_inheritance_binding"] = "not-proven", ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
    }

    public static int Run(string image, string report, string summary)
    {
        var paths = new[] { image, report, summary }.Select(Path.GetFullPath).ToArray();
        if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new ArgumentException("Image and output paths must differ.");
        var data = EaglMaterialDeclarationAudit.LoadSource(image);
        var elf = ElfReader.Inspect(data);
        var material = EaglMaterialDeclarationAudit.Inspect(data, elf);
        var section = elf.Sections[material.Section];
        var payload = data.AsSpan(checked((int)(section.Offset + material.VertexProgramOffset)), material.VertexProgramBytes);
        if (material.VertexProgramSha256 != ProgramSha256) throw new InvalidDataException("Vertex program identity changed.");
        var instructions = Decode(payload);
        var rows = instructions.Select(i => Row(i, material.VertexProgramSha256)).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(paths[1])!);
        using (var writer = new StreamWriter(paths[1], false, new UTF8Encoding(false)))
        {
            writer.WriteLine(string.Join("\t", rows[0].Keys));
            foreach (var row in rows) writer.WriteLine(string.Join("\t", row.Values));
        }
        var values = new Dictionary<string, string> {
            ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
            ["vertex_program_sha256"] = ProgramSha256, ["instruction_count"] = Num(instructions.Count),
            ["active_input_registers"] = string.Join(",", instructions.Where(i => i.ActiveSources.Any(s => s.Mux == 2)).Select(i => i.Input).Distinct().Order()),
            ["active_input_slot_references"] = Num(instructions.Sum(i => i.ActiveSources.Count(s => s.Mux == 2))),
            ["relative_constant_instructions"] = Num(instructions.Count(i => i.Relative && i.ActiveSources.Any(s => s.Mux == 3))),
            ["active_output_addresses"] = string.Join(",", instructions.Where(i => i.OutputMask != 0 && i.OutputBank == 1).Select(i => i.OutputAddress).Distinct().Order()),
            ["output_write_instructions"] = Num(instructions.Count(i => i.OutputMask != 0)),
            ["paired_instructions"] = Num(instructions.Count(i => i.Mac != 0 && i.Ilu != 0)),
            ["report_sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(paths[1]))),
            ["status"] = Status, ["operand_scope"] = "opcode-active-source-slots-not-component-liveness",
            ["parent_inheritance_binding"] = "not-proven", ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
        EaglRenderParameterAudit.WriteSummary(paths[2], values);
        foreach (var (key, value) in values) Console.WriteLine($"{key}: {value}");
        return 0;
    }

    private static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);
    private static string Hex(uint n) => $"0x{n:X8}";
    private static string Flag(bool v) => v ? "true" : "false";
}
