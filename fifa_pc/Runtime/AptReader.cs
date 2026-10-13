using System.Buffers.Binary;
using System.Text;

namespace FifaStreetPc;

public sealed record AptInspection(
    string Format,
    string VersionTag,
    uint HeaderSize,
    IReadOnlyList<uint> HeaderFields,
    bool FieldsWithinAsset);

public static class AptReader
{
    private static readonly byte[] DataMagic = "Apt Data:"u8.ToArray();
    private static readonly byte[] ConstantMagic = "Apt constant file"u8.ToArray();

    /// <summary>
    /// Returns a structural key for an APT entry and the files with the same
    /// stem in its nested BIG. It records only validated header metadata,
    /// extension presence, and byte sizes; it does not assign timing, pose,
    /// morph, material, or frame semantics to any field.
    /// </summary>
    public static string GetStructuralSignature(
        AptInspection inspection,
        IReadOnlyDictionary<string, uint> companionSizes)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(companionSizes);
        var fields = string.Join(",", inspection.HeaderFields.Select(field => $"0x{field:X}"));
        var companions = string.Join(",", companionSizes
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}=0x{pair.Value:X}"));
        return $"format={inspection.Format};version={inspection.VersionTag};header=0x{inspection.HeaderSize:X};fields={fields};within={inspection.FieldsWithinAsset};companions={companions}";
    }

    /// <summary>
    /// Extends the APT-only structural key with the independently validated
    /// constant-file header for the same stem. A missing constant inspection
    /// remains an explicit state; it is never treated as an animation rule.
    /// </summary>
    public static string GetCompanionStructuralSignature(
        AptInspection inspection,
        AptInspection? constantInspection,
        IReadOnlyDictionary<string, uint> companionSizes)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(companionSizes);
        var constant = constantInspection is null
            ? "absent-or-invalid"
            : $"format={constantInspection.Format};version={constantInspection.VersionTag};header=0x{constantInspection.HeaderSize:X};fields={string.Join(",", constantInspection.HeaderFields.Select(field => $"0x{field:X}"))};within={constantInspection.FieldsWithinAsset}";
        return $"{GetStructuralSignature(inspection, companionSizes)};constant={constant}";
    }

    /// <summary>
    /// Extends the APT/constant key with validated structural signatures for
    /// the same-stem EAGL object and SHPX package table. The EAGL key contains
    /// only section/symbol/relocation/class metadata and bounds presence. The
    /// SHPX key contains only entry-table metadata. Neither is interpreted as
    /// material or animation semantics.
    /// </summary>
    public static string GetPackageStructuralSignature(
        AptInspection inspection,
        AptInspection? constantInspection,
        IReadOnlyDictionary<string, uint> companionSizes,
        string? shapePackageSignature,
        string? objectStructuralSignature = null)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(companionSizes);
        return $"{GetCompanionStructuralSignature(inspection, constantInspection, companionSizes)};object={objectStructuralSignature ?? "absent-or-invalid"};shape={shapePackageSignature ?? "absent-or-invalid"}";
    }

    public static AptInspection Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length >= DataMagic.Length && data[..DataMagic.Length].SequenceEqual(DataMagic))
        {
            if (data.Length < 32)
            {
                throw new InvalidDataException("APT data header is truncated.");
            }

            var fields = new uint[4];
            for (var index = 0; index < fields.Length; index++)
            {
                fields[index] = BinaryPrimitives.ReadUInt32LittleEndian(data[(16 + index * 4)..(20 + index * 4)]);
            }

            var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]);
            var withinAsset = headerSize >= 16 && headerSize <= data.Length;
            foreach (var field in fields)
            {
                withinAsset &= field <= data.Length;
            }
            return new AptInspection(
                "Apt Data",
                Encoding.ASCII.GetString(data[9..10]),
                headerSize,
                fields,
                withinAsset);
        }

        if (data.Length >= ConstantMagic.Length && data[..ConstantMagic.Length].SequenceEqual(ConstantMagic))
        {
            if (data.Length < 32)
            {
                throw new InvalidDataException("APT constant header is truncated.");
            }

            var fields = new uint[2]
            {
                BinaryPrimitives.ReadUInt32LittleEndian(data[20..24]),
                BinaryPrimitives.ReadUInt32LittleEndian(data[28..32])
            };
            var headerSize = fields[0];
            var withinAsset = true;
            foreach (var field in fields)
            {
                withinAsset &= field <= data.Length;
            }
            return new AptInspection(
                "Apt constant",
                string.Empty,
                headerSize,
                fields,
                withinAsset);
        }

        throw new InvalidDataException("Unsupported APT signature.");
    }
}
