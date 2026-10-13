using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglMaterialSourceDefinition(string Kind, string ParentReference, string VertexName,
    string PixelName, EaglMaterialDeclaration Material);

/// <summary>
/// Explicit serialized model references, reconciled with fresh mat.o symbol
/// definitions. This is not a linker, an inherited runtime object, shader
/// installation, selected buffer reconstruction or original render execution.
/// </summary>
public static class EaglModelMaterialReferenceAudit
{
    public const string Status = "verified-static-model-declaration-and-named-program-reference";
    public const string DeclarationHash = "2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE";
    internal static readonly string[] Kinds = { "Skin", "Cloth", "Eye" };
    internal static readonly string[] ProgramHashes = { EaglVertexProgramAudit.ProgramSha256,
        "D17E05FE970D37E5D67B7FD3649AFC9F8F58D27CFE4EBF92DEA0737943CD363B",
        "307153C2F396F2FBAC94F855AF3BB6F82FD4CF1B9DFD6A264A90EE02B364B609" };

    private static ReadOnlySpan<byte> Slice(byte[] data, ElfInspection elf, int section, uint at, int count)
    {
        if (section <= 0 || section >= elf.Sections.Count || count < 0) throw new InvalidDataException("Invalid material section/length.");
        var s = elf.Sections[section];
        if (s.Type == 8 || (ulong)at + (uint)count > s.Size || (ulong)s.Offset + at + (uint)count > (ulong)data.Length)
            throw new InvalidDataException("Material field outside file-backed object.");
        return data.AsSpan(checked((int)((ulong)s.Offset + at)), count);
    }

    private static uint Local(byte[] data, ElfInspection elf, int section, uint at)
    {
        var refs = elf.Relocations.Where(r => r.TargetSectionIndex == section && r.Offset == at).ToArray();
        if (refs.Length != 1 || refs[0].Type != 2 || refs[0].SymbolName.Length != 0)
            throw new InvalidDataException("Material pointer is not unique and local.");
        return BinaryPrimitives.ReadUInt32LittleEndian(Slice(data, elf, section, at, 4));
    }

    private static string Named(byte[] data, ElfInspection elf, int section, uint at)
    {
        var refs = elf.Relocations.Where(r => r.TargetSectionIndex == section && r.Offset == at).ToArray();
        if (refs.Length != 1 || refs[0].Type != 2 || refs[0].SymbolName.Length == 0 ||
            BinaryPrimitives.ReadUInt32LittleEndian(Slice(data, elf, section, at, 4)) != 0)
            throw new InvalidDataException("Material reference is not a unique exact external symbol with zero addend.");
        return refs[0].SymbolName;
    }

    internal static IReadOnlyDictionary<string, EaglMaterialSourceDefinition> Definitions(byte[] data, ElfInspection elf)
    {
        var result = new Dictionary<string, EaglMaterialSourceDefinition>(StringComparer.Ordinal);
        for (var n = 0; n < Kinds.Length; n++)
        {
            var kind = Kinds[n];
            var parent = $"ParentRM_FifastCharacter{kind}";
            var vs = $"FifastCharacter{kind}0__EAGLMicroCodeVS";
            var ps = $"FifastCharacter{kind}0__EAGLMicroCodePS";
            var material = EaglMaterialDeclarationAudit.InspectNamed(data, elf, "__RM:::" + parent, vs);
            Slice(data, elf, material.Section, material.ParentOffset, 52);
            var psTable = Local(data, elf, material.Section, material.ParentOffset + 20);
            if (Named(data, elf, material.Section, psTable) != ps ||
                elf.Symbols.Count(s => s.Name == ps && s.SectionIndex == material.Section && s.SectionIndex != 0) != 1 ||
                material.DeclarationSha256 != DeclarationHash || material.VertexProgramSha256 != ProgramHashes[n])
                throw new InvalidDataException("Named material source definition changed.");
            var s = elf.Sections[material.Section];
            var instructions = EaglVertexProgramAudit.Decode(data.AsSpan(checked((int)(s.Offset + material.VertexProgramOffset)), material.VertexProgramBytes));
            if (instructions.Count != (kind == "Eye" ? 47 : 46)) throw new InvalidDataException("Named program count changed.");
            result.Add(vs, new(kind, parent, vs, ps, material));
        }
        return result;
    }

    internal static Dictionary<string, string> InspectPrimitive(EaglRawModelObject obj, EaglModelPrimitive primitive,
        IReadOnlyDictionary<string, EaglMaterialSourceDefinition> definitions)
    {
        var at = primitive.RenderMethodOffset;
        var sec = primitive.SourceSectionIndex;
        if ((at & 3) != 0) throw new InvalidDataException("Unaligned model render method.");
        var raw = Slice(obj.Data, obj.Elf, sec, at, 52);
        var words = new uint[13];
        for (var i = 0; i < words.Length; i++) words[i] = BinaryPrimitives.ReadUInt32LittleEndian(raw[(i * 4)..]);
        if (!primitive.RenderCommandStream.IsComplete || primitive.RenderCommandStream.Commands.Count == 0)
            throw new InvalidDataException("Incomplete model command source.");
        var commandOffset = primitive.RenderCommandStream.Commands[0].SectionOffset;
        if (Local(obj.Data, obj.Elf, sec, at) != commandOffset || words[1] != 1 ||
            words[4] != 0 || words[6] != 0 || words[9] != 0 || words[10] != uint.MaxValue)
            throw new InvalidDataException("Unsupported serialized model material prefix/state.");
        foreach (var field in new uint[] { 4, 16, 24, 36, 40 })
            if (obj.Elf.Relocations.Any(r => r.TargetSectionIndex == sec && (ulong)r.Offset < (ulong)at + field + 4 && (ulong)r.Offset + 4 > (ulong)at + field))
                throw new InvalidDataException("Relocation inside decoded raw material field.");
        var declaration = Local(obj.Data, obj.Elf, sec, at + 8);
        if ((declaration & 3) != 0) throw new InvalidDataException("Unaligned model declaration.");
        var section = obj.Elf.Sections[sec];
        if (declaration > section.Size) throw new InvalidDataException("Declaration outside section.");
        var layout = EaglMaterialDeclarationAudit.Decode(Slice(obj.Data, obj.Elf, sec, declaration, (int)Math.Min(1024u, section.Size - declaration)));
        if (obj.Elf.Relocations.Any(r => r.TargetSectionIndex == sec && (ulong)r.Offset < (ulong)declaration + (uint)layout.ConsumedBytes && (ulong)r.Offset + 4 > declaration))
            throw new InvalidDataException("Relocation in model declaration.");
        var vsTable = Local(obj.Data, obj.Elf, sec, at + 12);
        var psTable = Local(obj.Data, obj.Elf, sec, at + 20);
        var vsName = Named(obj.Data, obj.Elf, sec, vsTable);
        var psName = Named(obj.Data, obj.Elf, sec, psTable);
        var parent1C = Named(obj.Data, obj.Elf, sec, at + 28);
        var parent2C = Named(obj.Data, obj.Elf, sec, at + 44);
        if (!definitions.TryGetValue(vsName, out var definition) || psName != definition.PixelName ||
            parent1C != parent2C || parent2C != definition.ParentReference)
            throw new InvalidDataException("Unresolved or inconsistent named material references.");
        var declarationHash = Convert.ToHexString(SHA256.HashData(Slice(obj.Data, obj.Elf, sec, declaration, layout.ConsumedBytes)));
        if (declarationHash != definition.Material.DeclarationSha256)
            throw new InvalidDataException("Model/source declaration bytes differ.");
        return new() {
            ["model_archive"] = obj.ArchiveName, ["model_object"] = obj.ObjectName,
            ["object_sha256"] = Convert.ToHexString(SHA256.HashData(obj.Data)), ["section"] = Num(sec),
            ["layer"] = Num(primitive.LayerIndex), ["primitive"] = primitive.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            ["render_method_offset"] = Hex(at), ["render_method_bytes"] = "52", ["raw_words"] = string.Join(",", words.Select(Hex)),
            ["command_offset"] = Hex(commandOffset), ["declaration_offset"] = Hex(declaration),
            ["declaration_bytes"] = Num(layout.ConsumedBytes), ["declaration_sha256"] = declarationHash,
            ["declaration_elements"] = Num(layout.Elements.Count), ["vertex_table_offset"] = Hex(vsTable), ["pixel_table_offset"] = Hex(psTable),
            ["parent_1c_reference"] = parent1C, ["parent_2c_reference"] = parent2C, ["vertex_program_reference"] = vsName,
            ["pixel_program_reference"] = psName, ["material_kind"] = definition.Kind,
            ["definition_source"] = EaglMaterialDeclarationAudit.Source, ["definition_source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
            ["definition_parent_offset"] = Hex(definition.Material.ParentOffset), ["definition_vertex_offset"] = Hex(definition.Material.VertexProgramOffset),
            ["definition_vertex_bytes"] = Num(definition.Material.VertexProgramBytes), ["definition_vertex_instruction_count"] = Num(definition.Material.VertexInstructionCount),
            ["definition_vertex_sha256"] = definition.Material.VertexProgramSha256,
            ["status"] = Status, ["reference_scope"] = "serialized-reference-to-exact-named-program-definition",
            ["runtime_loader_resolution"] = "not-proven", ["selected_buffers"] = "not-proven",
            ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
    }

    public static int Run(string image, string report, string summary)
    {
        var paths = new[] { image, report, summary }.Select(Path.GetFullPath).ToArray();
        if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3) throw new ArgumentException("Inputs/outputs must differ.");
        var materialData = EaglMaterialDeclarationAudit.LoadSource(image);
        var definitions = Definitions(materialData, ElfReader.Inspect(materialData));
        var rows = new List<Dictionary<string, string>>();
        using var store = new GameAssetStore(image);
        var chain = EaglOriginalRenderChainAudit.Audit(store, "data/models.big", obj => {
            foreach (var primitive in obj.Primitives.OrderBy(p => p.LayerIndex).ThenBy(p => p.PrimitiveIndex))
                rows.Add(InspectPrimitive(obj, primitive, definitions));
        });
        if (chain.Summary.RejectedObjects != 0 || chain.Summary.RejectedPackages != 0 || rows.Count != chain.Summary.PrimitiveRows || rows.Count == 0)
            throw new InvalidDataException("Model material universe incomplete; no verified report written.");
        Directory.CreateDirectory(Path.GetDirectoryName(paths[1])!);
        using (var writer = new StreamWriter(paths[1], false, new UTF8Encoding(false)))
        {
            writer.WriteLine(string.Join("\t", rows[0].Keys));
            foreach (var row in rows) writer.WriteLine(string.Join("\t", row.Values));
        }
        var values = new Dictionary<string, string> {
            ["model_rows"] = Num(rows.Count), ["model_objects"] = Num(chain.Summary.ObjectsLoaded),
            ["material_definitions"] = Num(definitions.Count), ["declarations_matched"] = Num(rows.Count),
            ["parent_1c_2c_same_reference_rows"] = Num(rows.Count),
            ["skin_rows"] = Num(rows.Count(r => r["material_kind"] == "Skin")), ["cloth_rows"] = Num(rows.Count(r => r["material_kind"] == "Cloth")),
            ["eye_rows"] = Num(rows.Count(r => r["material_kind"] == "Eye")),
            ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
            ["declaration_sha256"] = DeclarationHash, ["report_sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(paths[1]))),
            ["status"] = Status, ["reference_scope"] = "serialized-reference-to-exact-named-program-definition",
            ["runtime_loader_resolution"] = "not-proven", ["selected_buffers"] = "not-proven", ["full_rendering_binding"] = "not-proven",
            ["original_semantics"] = "not-claimed" };
        EaglRenderParameterAudit.WriteSummary(paths[2], values);
        foreach (var (key, value) in values) Console.WriteLine($"{key}: {value}");
        return 0;
    }

    private static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);
    private static string Hex(uint n) => $"0x{n:X8}";
}
