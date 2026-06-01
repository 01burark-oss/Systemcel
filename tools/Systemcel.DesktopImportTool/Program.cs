using System.Net.Http.Headers;

namespace Systemcel.DesktopImportTool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = ImportToolOptions.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(ImportToolOptions.HelpText);
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(options.PackagePath))
            {
                if (!File.Exists(options.PackagePath))
                    throw new FileNotFoundException("Paket ZIP dosyasi bulunamadi.", options.PackagePath);

                if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || string.IsNullOrWhiteSpace(options.TransferCode))
                    throw new InvalidOperationException("--package icin --api ve --code zorunlu.");

                await UploadAsync(options.ApiBaseUrl, options.TransferCode, options.PackagePath);
                return 0;
            }

            var dbPath = options.DatabasePath ?? LegacySqliteReader.FindDefaultDatabasePath();
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new InvalidOperationException("SQLite dosyasi bulunamadi. --db <systemcel.db> ile dosya yolu verin.");

            if (!File.Exists(dbPath))
                throw new FileNotFoundException("SQLite dosyasi bulunamadi.", dbPath);

            if (options.Upload && string.IsNullOrWhiteSpace(options.ApiBaseUrl))
                throw new InvalidOperationException("--upload icin --api <url> zorunlu.");

            if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl) && string.IsNullOrWhiteSpace(options.TransferCode))
                throw new InvalidOperationException("--api ile yukleme icin --code MIG-000000 zorunlu.");

            Console.WriteLine($"SQLite okunuyor: {dbPath}");
            var reader = new LegacySqliteReader();
            var data = reader.Read(dbPath);

            var writer = new DesktopImportPackageWriter();
            var package = writer.Write(dbPath, options.OutputPath, options.TransferCode ?? string.Empty, data);

            Console.WriteLine($"Paket olusturuldu: {package.ZipPath}");
            Console.WriteLine($"Manifest: {package.Manifest.PackageId}");
            Console.WriteLine($"Isletme: {package.Manifest.Totals.Isletme}, Cari: {package.Manifest.Totals.CariKart}, Fatura: {package.Manifest.Totals.Fatura}, Kasa: {package.Manifest.Totals.KasaHareket}");

            if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl))
                await UploadAsync(options.ApiBaseUrl, options.TransferCode!, package.ZipPath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task UploadAsync(string apiBaseUrl, string transferCode, string packagePath)
    {
        var endpoint = new Uri(new Uri(apiBaseUrl.TrimEnd('/') + "/"), "api/import/desktop/packages");
        using var client = new HttpClient();
        await using var fileStream = File.OpenRead(packagePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(new StringContent(transferCode), "code");
        content.Add(fileContent, "package", Path.GetFileName(packagePath));

        Console.WriteLine($"Paket yukleniyor: {endpoint}");
        using var response = await client.PostAsync(endpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"API paketi kabul etmedi ({(int)response.StatusCode}): {body}");

        Console.WriteLine("API paketi kabul etti.");
        Console.WriteLine(body);
    }
}
