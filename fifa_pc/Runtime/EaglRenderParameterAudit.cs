using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglRenderParameterRow(
    string Archive, string Object, int Section, int Layer, uint Primitive,
    int CommandIndex, ushort CommandId, uint CommandOffset, uint ParameterCursor,
    int ConsumedBytes, uint Auxiliary, uint? Word0, uint? Word1,
    string Relocations, string TargetSymbols, string VertexBufferBaseSymbol,
    uint? Slot, uint? CommandStride, uint? BufferStride, string AddressScope,
    string RawTargetWords, uint? DrawCondition, string Status, string Issue);

/// <summary>
/// Parses the serialized parameter pairs consumed by the twelve observed
/// command handlers. The contract is checked against the pinned retail XBE
/// by eagl_render_parameter_static_control.py. This is a data parser: it
/// neither executes instructions nor reconstructs CPU or GPU state.
/// Runtime selector 0x345BCC stays symbolic. Matching a base record does not
/// establish the selected runtime buffer, vertex attribute or material.
/// </summary>
public static class EaglRenderParameterAudit
{
    public const string Status = "verified-bounded-eagl-command-parameter-layout";
    public const string OriginalSemantics = "not-claimed";

    public static bool TryGetContract(ushort id, out int words, out int parameterBytes)
    {
        words = id switch
        {
            2 or 4 or 14 or 16 or 31 => 2,
            15 or 20 or 26 => 3,
            5 or 6 or 9 => 4,
            29 => 6,
            _ => 0,
        };
        parameterBytes = words == 0 || id is 4 or 26 ? 0 : 8;
        return words != 0;
    }

    public static IReadOnlyList<EaglRenderParameterRow> Inspect(EaglRawModelObject obj)
    {
        var rows = new List<EaglRenderParameterRow>();
        var streams = EaglVertexBufferReader.Inspect(obj.Data, obj.Elf);
        foreach (var primitive in obj.Primitives.OrderBy(p => p.LayerIndex).ThenBy(p => p.PrimitiveIndex))
        {
            var cursor = primitive.GlobalParametersOffset;
            var limit = obj.Primitives.Where(p => p.SourceSectionIndex == primitive.SourceSectionIndex &&
                                                   p.RenderDescriptorOffset > primitive.RenderDescriptorOffset)
                .Select(p => p.RenderDescriptorOffset).DefaultIfEmpty(uint.MaxValue).Min();
            var issue = primitive.RenderCommandStream.IsComplete ? string.Empty : "incomplete-command-stream";
            foreach (var command in primitive.RenderCommandStream.Commands)
            {
                uint? word0 = null, word1 = null, slot = null, commandStride = null, bufferStride = null, drawCondition = null;
                var aux = 0u;
                var relocations = string.Empty;
                var symbols = string.Empty;
                var bufferSymbol = string.Empty;
                var rawWords = string.Empty;
                var scope = "no-parameter-pair";
                var known = TryGetContract(command.CommandId, out var words, out var consumed);
                if (issue.Length == 0 && (!known || command.SizeWords != words ||
                    command.RawBytes.Length != words * 4 ||
                    BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes) !=
                        ((uint)command.CommandId << 16 | command.SizeWords)))
                {
                    issue = "unknown-command-or-invalid-command-size/header";
                }
                if (issue.Length == 0)
                {
                    aux = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(4));
                    if (command.CommandId == 26)
                        drawCondition = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(8));
                    if (consumed != 0)
                    {
                        if ((ulong)cursor + 8 > limit ||
                            !TryReadWord(obj, primitive.SourceSectionIndex, cursor, out var first) ||
                            cursor > uint.MaxValue - 4 ||
                            !TryReadWord(obj, primitive.SourceSectionIndex, cursor + 4, out var second))
                        {
                            issue = "parameter-pair-outside-section-or-next-descriptor";
                        }
                        else
                        {
                            word0 = first;
                            word1 = second;
                            var targetRelocations = obj.Elf.Relocations.Where(r =>
                                r.TargetSectionIndex == primitive.SourceSectionIndex && r.Offset == cursor + 4).ToArray();
                            relocations = string.Join("|", targetRelocations.Select(r => $"{r.Type}:{r.SymbolName}"));
                            // Named relocations are unresolved external symbol bases. A raw
                            // zero there is not a pointer to the start of this data section.
                            var localBase = targetRelocations.Length == 1 && targetRelocations[0].Type == 2 &&
                                            targetRelocations[0].SymbolName.Length == 0;
                            if (localBase)
                                symbols = string.Join("|", obj.Elf.Symbols.Where(s =>
                                        s.SectionIndex == primitive.SourceSectionIndex && s.Value == second && s.Name.Length > 0)
                                    .Select(s => s.Name).Distinct(StringComparer.Ordinal));
                            scope = localBase
                                ? (aux == 0 ? "local-base-independent-of-runtime-selector" : "local-base-only-runtime-selector-unresolved")
                                : "external-or-unresolved-relocation";
                            var targetWords = new List<string>();
                            for (var j = 0u; j < 4; j++)
                                if (localBase && (ulong)second + j * 4 <= uint.MaxValue &&
                                    TryReadWord(obj, primitive.SourceSectionIndex, second + j * 4, out var value))
                                    targetWords.Add($"0x{value:X8}");
                            rawWords = string.Join(",", targetWords);
                            if (command.CommandId == 9)
                            {
                                slot = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(8));
                                commandStride = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(12));
                                // XBE handler forms pair[1] + aux*selector - 20.
                                // Only the unadjusted base is matched here.
                                var matches = streams.Where(s => localBase && s.SectionIndex == primitive.SourceSectionIndex &&
                                    (ulong)s.BufferRecordOffset + 20 == second).ToArray();
                                if (matches.Length == 1)
                                {
                                    bufferSymbol = matches[0].SymbolName;
                                    bufferStride = matches[0].Stride;
                                }
                            }
                        }
                    }
                }
                rows.Add(new EaglRenderParameterRow(
                    obj.ArchiveName, obj.ObjectName, primitive.SourceSectionIndex,
                    primitive.LayerIndex, primitive.PrimitiveIndex, command.Index, command.CommandId,
                    command.SectionOffset, cursor, consumed, aux, word0, word1,
                    relocations, symbols, bufferSymbol, slot, commandStride, bufferStride, scope,
                    rawWords, drawCondition, issue.Length == 0 ? Status : "rejected-eagl-command-parameter-layout", issue));
                // Fail closed after the first invalid command; later cursor positions are not recoverable.
                if (issue.Length == 0)
                    cursor = checked(cursor + (uint)consumed);
            }
        }
        return rows;
    }

    private static bool TryReadWord(EaglRawModelObject obj, int sectionIndex, uint offset, out uint word)
    {
        word = 0;
        if (sectionIndex <= 0 || sectionIndex >= obj.Elf.Sections.Count)
            return false;
        var section = obj.Elf.Sections[sectionIndex];
        if (section.Type == 8 || (ulong)offset + 4 > section.Size ||
            (ulong)section.Offset + offset + 4 > (ulong)obj.Data.Length)
            return false;
        word = BinaryPrimitives.ReadUInt32LittleEndian(obj.Data.AsSpan(checked((int)(section.Offset + offset)), 4));
        return true;
    }

    public static int Run(string image, string report, string summary)
    {
        var source = Path.GetFullPath(image);
        if (new[] { report, summary }.Any(p => string.Equals(Path.GetFullPath(p), source, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(Path.GetFullPath(report), Path.GetFullPath(summary), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Report paths must differ from the ISO and each other.");
        using var store = new GameAssetStore(image);
        var rows = new List<EaglRenderParameterRow>();
        var chain = EaglOriginalRenderChainAudit.Audit(store, "data/models.big", obj => rows.AddRange(Inspect(obj)));
        WriteReport(report, rows);
        var values = Summarize(rows);
        values["source_chain_status"] = chain.Summary.Status;
        values["objects_loaded"] = chain.Summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture);
        values["status"] = rows.Count > 0 && rows.All(r => r.Status == Status) &&
                           chain.Summary.Status == EaglOriginalRenderChainAudit.Status
            ? Status : "incomplete-eagl-command-parameter-layout";
        WriteSummary(summary, values);
        foreach (var (key, value) in values)
            Console.WriteLine($"{key}: {value}");
        return values["status"] == Status ? 0 : 1;
    }

    public static Dictionary<string, string> Summarize(IReadOnlyList<EaglRenderParameterRow> rows) => new()
    {
        ["primitive_rows"] = rows.Select(r => (r.Archive, r.Object, r.Section, r.Layer, r.Primitive)).Distinct().Count().ToString(CultureInfo.InvariantCulture),
        ["command_rows"] = rows.Count.ToString(CultureInfo.InvariantCulture),
        ["valid_command_rows"] = rows.Count(r => r.Status == Status).ToString(CultureInfo.InvariantCulture),
        ["parameter_pairs"] = rows.Count(r => r.Word1.HasValue).ToString(CultureInfo.InvariantCulture),
        ["fetch_commands"] = rows.Count(r => r.CommandId == 9).ToString(CultureInfo.InvariantCulture),
        ["fetch_base_record_matches"] = rows.Count(r => r.VertexBufferBaseSymbol.Length > 0).ToString(CultureInfo.InvariantCulture),
        ["fetch_stride_matches"] = rows.Count(r => r.BufferStride.HasValue && r.BufferStride == r.CommandStride).ToString(CultureInfo.InvariantCulture),
        ["fetch_runtime_selector_unresolved"] = rows.Count(r => r.CommandId == 9 && r.Auxiliary != 0).ToString(CultureInfo.InvariantCulture),
        ["draw_commands"] = rows.Count(r => r.CommandId == 26).ToString(CultureInfo.InvariantCulture),
        ["draw_condition_nonzero"] = rows.Count(r => r.DrawCondition > 0).ToString(CultureInfo.InvariantCulture),
        ["draw_condition_zero"] = rows.Count(r => r.DrawCondition == 0).ToString(CultureInfo.InvariantCulture),
        ["tar_state_parameters"] = rows.Count(r => r.CommandId == 15 && r.Relocations.StartsWith("2:__EAGL::TAR:::", StringComparison.Ordinal) &&
            r.Relocations.Contains("SHAPENAME=", StringComparison.Ordinal)).ToString(CultureInfo.InvariantCulture),
        ["geo_state_parameters"] = rows.Count(r => r.CommandId == 16 && r.Relocations.StartsWith("2:__EAGL::GeoPrimState:::", StringComparison.Ordinal)).ToString(CultureInfo.InvariantCulture),
        ["primitives_with_both_state_parameters"] = rows.GroupBy(r => (r.Archive, r.Object, r.Section, r.Layer, r.Primitive))
            .Count(g => g.Any(r => r.CommandId == 15 && r.Relocations.Contains("SHAPENAME=", StringComparison.Ordinal)) &&
                        g.Any(r => r.CommandId == 16 && r.Relocations.StartsWith("2:__EAGL::GeoPrimState:::", StringComparison.Ordinal))).ToString(CultureInfo.InvariantCulture),
        ["original_semantics"] = OriginalSemantics,
    };

    public static void WriteReport(string path, IEnumerable<EaglRenderParameterRow> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("model_archive\tmodel_object\tsection\tlayer\tprimitive\tcommand_index\tcommand_id\tcommand_offset\tparameter_cursor\tconsumed_bytes\tauxiliary\tparameter_word0\tparameter_word1\tparameter_relocations\ttarget_symbols\tvb_base_symbol\tslot\tcommand_stride\tbuffer_stride\taddress_scope\traw_target_words\tdraw_condition\tstatus\toriginal_semantics\tissue");
        foreach (var r in rows)
            writer.WriteLine(string.Join("\t", new object?[] {
                r.Archive, r.Object, r.Section, r.Layer, r.Primitive, r.CommandIndex, r.CommandId,
                Hex(r.CommandOffset), Hex(r.ParameterCursor), r.ConsumedBytes, Hex(r.Auxiliary),
                Hex(r.Word0), Hex(r.Word1), r.Relocations, r.TargetSymbols, r.VertexBufferBaseSymbol,
                r.Slot, r.CommandStride, r.BufferStride, r.AddressScope, r.RawTargetWords,
                r.DrawCondition, r.Status, OriginalSemantics, r.Issue,
            }.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ') ?? string.Empty)));
    }

    private static string Hex(uint? value) => value.HasValue ? $"0x{value.Value:X8}" : string.Empty;

    public static void WriteSummary(string path, Dictionary<string, string> summary)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllLines(path, new[] { string.Join("\t", summary.Keys), string.Join("\t", summary.Values) }, new UTF8Encoding(false));
    }
}
