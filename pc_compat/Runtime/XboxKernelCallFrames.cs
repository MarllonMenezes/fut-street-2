namespace FifaPcCompat.Runtime;

public sealed record XboxPsCreateSystemThreadExCall(IReadOnlyList<nint> Arguments)
{
    public const int ObservedArgumentCount = 10;

    public static XboxPsCreateSystemThreadExCall FromObservedStartup(params nint[] arguments)
    {
        if (arguments.Length != ObservedArgumentCount)
        {
            throw new ArgumentException($"Expected {ObservedArgumentCount} raw arguments.", nameof(arguments));
        }

        return new XboxPsCreateSystemThreadExCall(arguments.ToArray());
    }

    // The startup disassembly proves the stack shape and values, but not the
    // Xbox-specific parameter names. Keep these as positional values.
    public nint Argument0 => Arguments[0];
    public nint Argument1 => Arguments[1];
    public nint Argument2 => Arguments[2];
    public nint Argument3 => Arguments[3];
    public nint Argument4 => Arguments[4];
    public nint Argument5 => Arguments[5];
    public nint Argument6 => Arguments[6];
    public nint Argument7 => Arguments[7];
    public nint Argument8 => Arguments[8];
    public nint Argument9 => Arguments[9];
}
