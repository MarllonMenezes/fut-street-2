using FifaPcCompat.Runtime;

var host = new CompatibilityHost();
var command = args.Length == 0 ? "status" : args[0].ToLowerInvariant();

switch (command)
{
    case "status":
        Console.WriteLine(host.Status());
        break;
    case "validate-contract":
        host.ValidateContracts();
        Console.WriteLine("compatibility contracts: OK");
        break;
    case "inspect":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Uso: FifaPcCompat inspect <caminho-do-iso>");
            return 2;
        }
        Console.WriteLine(host.InspectImage(args[1]));
        break;
    case "fs-smoke":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Uso: FifaPcCompat fs-smoke <caminho-do-iso> [arquivo-virtual]");
            return 2;
        }
        Console.WriteLine(host.FileSystemSmoke(args[1], args.Length >= 3 ? args[2] : "default.xbe"));
        break;
    case "big-smoke":
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Uso: FifaPcCompat big-smoke <caminho-do-iso> <arquivo-big>");
            return 2;
        }
        Console.WriteLine(host.BigArchiveSmoke(args[1], args[2]));
        break;
    case "kernel-smoke":
        Console.WriteLine(host.KernelSmoke());
        break;
    case "kernel-abi-smoke":
        Console.WriteLine(host.KernelAbiSmoke());
        break;
    case "kernel-scheduler-smoke":
        Console.WriteLine(host.KernelSchedulerSmoke());
        break;
    case "cpu-probe":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Uso: FifaPcCompat cpu-probe <caminho-do-iso> [passos]");
            return 2;
        }
        var maxSteps = args.Length >= 3 && int.TryParse(args[2], out var parsedSteps) ? parsedSteps : 500;
        Console.WriteLine(host.CpuProbe(args[1], maxSteps));
        break;
    default:
        Console.Error.WriteLine("Uso: FifaPcCompat [status|validate-contract]");
        return 2;
}

return 0;
