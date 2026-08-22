using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.Extensions.Logging;

namespace CashTracker.Infrastructure.Services;

public sealed class NetgsmMusteriSmsSender : IMusteriSmsSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly MusteriSmsSettings _settings;
    private readonly ILogger<NetgsmMusteriSmsSender> _logger;

    public NetgsmMusteriSmsSender(
        HttpClient httpClient,
        MusteriSmsSettings settings,
        ILogger<NetgsmMusteriSmsSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task<MusteriSmsGonderimSonucu> SendAsync(
        string phoneNumber,
        string message,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new MusteriSmsGonderimSonucu(false, "Netgsm", string.Empty, "Netgsm SMS ayarları eksik.");
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10 || phoneNumber[0] != '5')
            return new MusteriSmsGonderimSonucu(false, "Netgsm", string.Empty, "Telefon numarası 5XXXXXXXXX biçiminde olmalıdır.");
        if (string.IsNullOrWhiteSpace(message))
            return new MusteriSmsGonderimSonucu(false, "Netgsm", string.Empty, "SMS metni boş olamaz.");

        var payload = new
        {
            msgheader = _settings.Header.Trim(),
            appname = string.IsNullOrWhiteSpace(_settings.AppName) ? null : _settings.AppName.Trim(),
            iysfilter = "0",
            encoding = "TR",
            messages = new[] { new { msg = message.Trim(), no = phoneNumber } }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.EffectiveBaseUrl}/sms/rest/v2/send");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{_settings.Username.Trim()}:{_settings.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            NetgsmResponse? providerResponse = null;
            try
            {
                providerResponse = JsonSerializer.Deserialize<NetgsmResponse>(body, JsonOptions);
            }
            catch (JsonException)
            {
                // Aşağıdaki güvenli hata metniyle devam edilir.
            }

            if (response.IsSuccessStatusCode && providerResponse?.Code == "00")
            {
                return new MusteriSmsGonderimSonucu(
                    true,
                    "Netgsm",
                    providerResponse.JobId ?? string.Empty,
                    string.Empty);
            }

            var error = providerResponse?.Description;
            if (string.IsNullOrWhiteSpace(error))
                error = $"Netgsm HTTP {(int)response.StatusCode} yanıtı verdi.";
            _logger.LogWarning(
                "Netgsm müşteri teyit SMS'i gönderilemedi. Status={StatusCode} Code={ProviderCode}",
                (int)response.StatusCode,
                providerResponse?.Code ?? "unknown");
            return new MusteriSmsGonderimSonucu(false, "Netgsm", string.Empty, error.Trim());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Netgsm müşteri teyit SMS'i gönderilirken ağ hatası oluştu.");
            return new MusteriSmsGonderimSonucu(false, "Netgsm", string.Empty, "SMS sağlayıcısına ulaşılamadı.");
        }
    }

    private sealed class NetgsmResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? JobId { get; set; }
    }
}

public sealed class UnconfiguredMusteriSmsSender : IMusteriSmsSender
{
    public bool IsConfigured => false;

    public Task<MusteriSmsGonderimSonucu> SendAsync(
        string phoneNumber,
        string message,
        CancellationToken ct = default) =>
        Task.FromResult(new MusteriSmsGonderimSonucu(
            false,
            "Yapılandırılmadı",
            string.Empty,
            "Müşteri SMS gönderimi henüz yapılandırılmamış."));
}
