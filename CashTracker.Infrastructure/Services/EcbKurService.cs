using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services;

public sealed class EcbKurService(HttpClient httpClient) : IEcbKurService
{
    private static readonly Uri FeedUrl = new("https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml");
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private EcbKurBulteniDto? _cached;
    private DateTimeOffset _refreshAfter;

    public async Task<EcbKurBulteniDto> GetLatestAsync(CancellationToken ct = default)
    {
        if (UsableCache() && DateTimeOffset.UtcNow < _refreshAfter) return _cached!;

        await _refreshLock.WaitAsync(ct);
        try
        {
            if (UsableCache() && DateTimeOffset.UtcNow < _refreshAfter) return _cached!;

            using var response = await httpClient.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > 256_000)
                throw new InvalidDataException("ECB bülteni beklenenden büyük.");

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 256_000
            });
            var document = await XDocument.LoadAsync(reader, LoadOptions.None, ct);
            var bulletin = Parse(document);
            _cached = bulletin;
            _refreshAfter = DateTimeOffset.UtcNow.Add(CacheDuration);
            return bulletin;
        }
        catch (Exception ex) when (UsableCache() && !ct.IsCancellationRequested &&
                                   ex is HttpRequestException or InvalidDataException or XmlException or OperationCanceledException)
        {
            _refreshAfter = DateTimeOffset.UtcNow.AddMinutes(5);
            return _cached!;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool UsableCache() => _cached is not null &&
        DateOnly.TryParseExact(_cached.Tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
        date >= DateOnly.FromDateTime(DateTime.Now.AddDays(-10));

    internal static EcbKurBulteniDto Parse(XDocument document)
    {
        var root = document.Root;
        var dailyRows = root?.Descendants().Where(x => x.Name.LocalName == "Cube" && x.Attribute("time") is not null).Take(2).ToArray();
        if (dailyRows?.Length != 1) throw new InvalidDataException("ECB bülten biçimi geçersiz.");
        var daily = dailyRows[0];
        var dateText = (string?)daily?.Attribute("time");
        if (root?.Name.LocalName != "Envelope" ||
            !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            date > DateOnly.FromDateTime(DateTime.Now) ||
            date < DateOnly.FromDateTime(DateTime.Now.AddDays(-10)))
            throw new InvalidDataException("ECB bülten tarihi geçersiz.");

        var euroRates = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var currency in daily!.Elements().Where(x => x.Name.LocalName == "Cube"))
        {
            var code = ((string?)currency.Attribute("currency"))?.Trim().ToUpperInvariant();
            if (code is null || code.Length != 3 || !code.All(c => c is >= 'A' and <= 'Z')) continue;
            if (!decimal.TryParse((string?)currency.Attribute("rate"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
                continue;
            if (!euroRates.TryAdd(code, rate)) throw new InvalidDataException("ECB bülteninde yinelenen para birimi var.");
        }

        if (!euroRates.TryGetValue("TRY", out var tryPerEuro) || tryPerEuro <= 0 || tryPerEuro > 1_000_000m)
            throw new InvalidDataException("ECB bülteninde TRY kuru bulunamadı.");

        var rates = new List<EcbKurDto> { new() { ParaBirimi = "EUR", Kur = tryPerEuro } };
        foreach (var (currency, euroRate) in euroRates.Where(x => x.Key is not ("TRY" or "EUR")))
        {
            if (euroRate < tryPerEuro / 1_000_000m) continue;
            var tryRate = decimal.Round(tryPerEuro / euroRate, 6, MidpointRounding.AwayFromZero);
            if (tryRate <= 0 || tryRate > 1_000_000m) continue;
            rates.Add(new EcbKurDto { ParaBirimi = currency, Kur = tryRate });
        }

        return new EcbKurBulteniDto { Tarih = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Kurlar = rates };
    }
}
