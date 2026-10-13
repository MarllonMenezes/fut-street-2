namespace FifaPcCompat.Runtime;

public sealed record XbeSectionLayout(
    string Name,
    uint VirtualAddress,
    uint VirtualSize,
    uint FileAddress,
    uint FileSize,
    bool Executable,
    bool Writable);

public sealed record XbeLoadPlan(
    uint BaseAddress,
    uint EntryPoint,
    IReadOnlyList<XbeSectionLayout> Sections)
{
    public void Validate()
    {
        if (!Sections.Any(section => section.Executable &&
            section.VirtualAddress <= EntryPoint &&
            EntryPoint < section.VirtualAddress + section.VirtualSize))
        {
            throw new InvalidOperationException("Entry point is outside executable sections.");
        }

        var ordered = Sections.OrderBy(section => section.VirtualAddress).ToArray();
        for (var index = 1; index < ordered.Length; index++)
        {
            var previousEnd = ordered[index - 1].VirtualAddress + ordered[index - 1].VirtualSize;
            if (previousEnd > ordered[index].VirtualAddress)
            {
                throw new InvalidOperationException("XBE virtual sections overlap.");
            }
        }
    }
}
