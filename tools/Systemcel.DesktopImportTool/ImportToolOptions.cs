namespace Systemcel.DesktopImportTool;

internal sealed class ImportToolOptions
{
    public string? DatabasePath { get; private set; }
    public string? OutputPath { get; private set; }
    public string? PackagePath { get; private set; }
    public string? TransferCode { get; private set; }
    public string? ApiBaseUrl { get; private set; }
    public bool Upload { get; private set; }
    public bool ShowHelp { get; private set; }

    public const string HelpText = """
Systemcel desktop import tool

Usage:
  dotnet run --project tools/Systemcel.DesktopImportTool -- --db C:\path\systemcel.db --code MIG-123456 --out .\tmp\import
  dotnet run --project tools/Systemcel.DesktopImportTool -- --db C:\path\systemcel.db --code MIG-123456 --api http://localhost:5000 --upload

Options:
  --db       Source SQLite database path. If omitted, common LOCALAPPDATA paths are scanned.
  --code     Transfer code created by Systemcel.Api.
  --out      Output ZIP path or output directory. Defaults to current directory.
  --package  Existing import ZIP to upload without reading SQLite again.
  --api      Systemcel.Api base URL.
  --upload   Upload the generated package to the API.
  --help     Show this help.
""";

    public static ImportToolOptions Parse(string[] args)
    {
        var options = new ImportToolOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--db":
                    options.DatabasePath = RequireValue(args, ref i, arg);
                    break;
                case "--out":
                    options.OutputPath = RequireValue(args, ref i, arg);
                    break;
                case "--package":
                    options.PackagePath = RequireValue(args, ref i, arg);
                    break;
                case "--code":
                    options.TransferCode = RequireValue(args, ref i, arg);
                    break;
                case "--api":
                    options.ApiBaseUrl = RequireValue(args, ref i, arg);
                    options.Upload = true;
                    break;
                case "--upload":
                    options.Upload = true;
                    break;
                default:
                    throw new ArgumentException($"Bilinmeyen arguman: {arg}");
            }
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"{option} icin deger bekleniyor.");

        index++;
        return args[index];
    }
}
