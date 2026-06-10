using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class GibPortalClient : IGibPortalClient
    {
        private readonly HttpClient _httpClient;

        public GibPortalClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GibPortalResult> TestLoginAsync(
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(kullaniciKodu) || string.IsNullOrWhiteSpace(sifre))
                return GibPortalResult.Fail("GİB kullanıcı kodu ve şifre gerekli.");

            try
            {
                var token = await LoginAsync(kullaniciKodu, sifre, testModu, ct);
                await LogoutAsync(token, testModu, ct);
                return GibPortalResult.Ok("GİB Portal girişi doğrulandı.");
            }
            catch (Exception ex)
            {
                return GibPortalResult.Fail(Sanitize(ex.Message));
            }
        }

        public Task<GibPortalResult> CreateDraftAsync(
            FaturaDetail fatura,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct = default)
        {
            if (fatura.Fatura.Id <= 0)
                return Task.FromResult(GibPortalResult.Fail("Fatura bulunamadı."));

            if (fatura.Fatura.FaturaTipi != "Satis")
                return Task.FromResult(GibPortalResult.Fail("GİB e-Arşiv Portal'a V1'de sadece satış faturası taslağı gönderilir."));

            return CreateDraftCoreAsync(fatura, kullaniciKodu, sifre, testModu, ct);
        }

        public Task<GibPortalResult> StartSmsVerificationAsync(
            string uuid,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return Task.FromResult(GibPortalResult.Fail("Portal UUID bulunamadı."));

            return StartSmsVerificationCoreAsync(uuid, kullaniciKodu, sifre, testModu, ct);
        }

        public Task<GibPortalResult> CompleteSmsVerificationAsync(
            string uuid,
            string operationId,
            string smsCode,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(operationId) || string.IsNullOrWhiteSpace(smsCode))
                return Task.FromResult(GibPortalResult.Fail("SMS onay bilgileri eksik."));

            return CompleteSmsVerificationCoreAsync(uuid, operationId, smsCode, kullaniciKodu, sifre, testModu, ct);
        }

        private async Task<GibPortalResult> CreateDraftCoreAsync(
            FaturaDetail fatura,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct)
        {
            string? token = null;
            try
            {
                token = await LoginAsync(kullaniciKodu, sifre, testModu, ct);
                var uuid = string.IsNullOrWhiteSpace(fatura.Fatura.PortalUuid)
                    ? Guid.NewGuid().ToString()
                    : fatura.Fatura.PortalUuid;
                var payload = BuildInvoicePayload(fatura, uuid);
                using var response = await DispatchAsync(
                    testModu,
                    token,
                    "EARSIV_PORTAL_FATURA_OLUSTUR",
                    "RG_BASITFATURA",
                    payload,
                    ct);

                var message = ExtractMessage(response);
                if (IsSuccessMessage(message))
                    return GibPortalResult.Ok("GİB Portal taslağı oluşturuldu.", uuid);

                return GibPortalResult.Fail(string.IsNullOrWhiteSpace(message)
                    ? "GİB Portal taslak gönderimi başarısız."
                    : message);
            }
            catch (Exception ex)
            {
                return GibPortalResult.Fail(Sanitize(ex.Message));
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(token))
                    await LogoutBestEffortAsync(token, testModu);
            }
        }

        private async Task<GibPortalResult> StartSmsVerificationCoreAsync(
            string uuid,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct)
        {
            string? token = null;
            try
            {
                token = await LoginAsync(kullaniciKodu, sifre, testModu, ct);
                using var phoneResponse = await DispatchAsync(
                    testModu,
                    token,
                    "EARSIV_PORTAL_TELEFONNO_SORGULA",
                    "RG_BASITTASLAKLAR",
                    new Dictionary<string, object?>(),
                    ct);
                var phone = ExtractDataProperty(phoneResponse, "telefon");
                if (string.IsNullOrWhiteSpace(phone))
                    return GibPortalResult.Fail("GİB Portal kayıtlı telefon numarası dönmedi.");

                using var smsResponse = await DispatchAsync(
                    testModu,
                    token,
                    "EARSIV_PORTAL_SMSSIFRE_GONDER",
                    "RG_SMSONAY",
                    new Dictionary<string, object?>
                    {
                        ["CEPTEL"] = phone,
                        ["KCEPTEL"] = false,
                        ["TIP"] = ""
                    },
                    ct);
                var operationId = ExtractDataProperty(smsResponse, "oid");
                if (string.IsNullOrWhiteSpace(operationId))
                    return GibPortalResult.Fail(ExtractMessage(smsResponse));

                return GibPortalResult.Ok("SMS kodu GİB Portal'daki kayıtlı telefona gönderildi.", uuid, operationId: operationId);
            }
            catch (Exception ex)
            {
                return GibPortalResult.Fail(Sanitize(ex.Message));
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(token))
                    await LogoutBestEffortAsync(token, testModu);
            }
        }

        private async Task<GibPortalResult> CompleteSmsVerificationCoreAsync(
            string uuid,
            string operationId,
            string smsCode,
            string kullaniciKodu,
            string sifre,
            bool testModu,
            CancellationToken ct)
        {
            string? token = null;
            try
            {
                token = await LoginAsync(kullaniciKodu, sifre, testModu, ct);
                using var response = await DispatchAsync(
                    testModu,
                    token,
                    "0lhozfib5410mp",
                    "RG_SMSONAY",
                    new Dictionary<string, object?>
                    {
                        ["DATA"] = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["belgeTuru"] = "FATURA",
                                ["ettn"] = uuid
                            }
                        },
                        ["SIFRE"] = smsCode.Trim(),
                        ["OID"] = operationId.Trim(),
                        ["OPR"] = 1
                    },
                    ct);

                var result = ExtractDataProperty(response, "sonuc");
                if (string.Equals(result, "1", StringComparison.Ordinal))
                    return GibPortalResult.Ok("GİB Portal SMS onayı tamamlandı.", uuid);

                var message = ExtractMessage(response);
                return GibPortalResult.Fail(string.IsNullOrWhiteSpace(message) ? "GİB Portal SMS onayı başarısız." : message);
            }
            catch (Exception ex)
            {
                return GibPortalResult.Fail(Sanitize(ex.Message));
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(token))
                    await LogoutBestEffortAsync(token, testModu);
            }
        }

        private async Task<string> LoginAsync(string kullaniciKodu, string sifre, bool testModu, CancellationToken ct)
        {
            var fields = new Dictionary<string, string>
            {
                ["assoscmd"] = testModu ? "login" : "anologin",
                ["rtype"] = "json",
                ["userid"] = kullaniciKodu.Trim(),
                ["sifre"] = sifre,
                ["sifre2"] = sifre,
                ["parola"] = sifre
            };

            using var content = new FormUrlEncodedContent(fields);
            using var response = await _httpClient.PostAsync(GetLoginUri(testModu), content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GİB login başarısız: {(int)response.StatusCode}");

            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException(ExtractText(error));

            if (!json.RootElement.TryGetProperty("token", out var tokenElement))
                throw new InvalidOperationException("GİB login token dönmedi.");

            var token = tokenElement.GetString();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("GİB login token boş döndü.");

            return token;
        }

        private async Task LogoutAsync(string token, bool testModu, CancellationToken ct)
        {
            var fields = new Dictionary<string, string>
            {
                ["assoscmd"] = "logout",
                ["token"] = token
            };
            using var content = new FormUrlEncodedContent(fields);
            using var _ = await _httpClient.PostAsync(GetLoginUri(testModu), content, ct);
        }

        private async Task LogoutBestEffortAsync(string token, bool testModu)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await LogoutAsync(token, testModu, cts.Token);
            }
            catch
            {
            }
        }

        private async Task<JsonDocument> DispatchAsync(
            bool testModu,
            string token,
            string command,
            string pageName,
            Dictionary<string, object?> payload,
            CancellationToken ct)
        {
            var fields = new Dictionary<string, string>
            {
                ["cmd"] = command,
                ["callid"] = Guid.NewGuid().ToString(),
                ["pageName"] = pageName,
                ["token"] = token,
                ["jp"] = JsonSerializer.Serialize(payload, JsonOptions)
            };

            using var content = new FormUrlEncodedContent(fields);
            using var response = await _httpClient.PostAsync(GetDispatchUri(testModu), content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GİB dispatch başarısız: {(int)response.StatusCode}");

            return JsonDocument.Parse(body);
        }

        private static Dictionary<string, object?> BuildInvoicePayload(FaturaDetail detail, string uuid)
        {
            var fatura = detail.Fatura;
            var cari = detail.Cari ?? throw new InvalidOperationException("Fatura carisi bulunamadı.");
            if (string.IsNullOrWhiteSpace(cari.VergiNoTc))
                throw new InvalidOperationException("GİB taslak için cari Vergi/TC no gerekli.");

            var (firstName, lastName) = SplitName(cari.Unvan);
            var rows = new List<Dictionary<string, object?>>();
            foreach (var line in detail.Satirlar)
            {
                rows.Add(new Dictionary<string, object?>
                {
                    ["malHizmet"] = line.Aciklama,
                    ["miktar"] = Amount(line.Miktar),
                    ["birim"] = UnitCode(line.Birim),
                    ["birimFiyat"] = Amount(line.BirimFiyat),
                    ["fiyat"] = Amount(line.Miktar * line.BirimFiyat),
                    ["iskontoOrani"] = Amount(line.IskontoOrani),
                    ["iskontoTutari"] = Amount(line.IskontoTutar),
                    ["iskontoNedeni"] = "",
                    ["malHizmetTutari"] = Amount(line.SatirNetTutar),
                    ["kdvOrani"] = Amount(line.KdvOrani),
                    ["vergiOrani"] = "0",
                    ["kdvTutari"] = Amount(line.KdvTutar),
                    ["vergininKdvTutari"] = "0",
                    ["ozelMatrahTutari"] = "0",
                    ["hesaplananotvtevkifatakatkisi"] = "0"
                });
            }

            return new Dictionary<string, object?>
            {
                ["faturaUuid"] = uuid,
                ["belgeNumarasi"] = "",
                ["faturaTarihi"] = fatura.Tarih.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                ["saat"] = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                ["paraBirimi"] = "TRY",
                ["dovzTLkur"] = "0",
                ["faturaTipi"] = "SATIS",
                ["hangiTip"] = "5000/30000",
                ["vknTckn"] = cari.VergiNoTc.Trim(),
                ["aliciUnvan"] = IsLikelyCompany(cari.VergiNoTc) ? cari.Unvan : "",
                ["aliciAdi"] = IsLikelyCompany(cari.VergiNoTc) ? "" : firstName,
                ["aliciSoyadi"] = IsLikelyCompany(cari.VergiNoTc) ? "" : lastName,
                ["binaAdi"] = "",
                ["binaNo"] = "",
                ["kapiNo"] = "",
                ["kasabaKoy"] = "",
                ["vergiDairesi"] = cari.VergiDairesi,
                ["ulke"] = "T\u00fcrkiye",
                ["bulvarcaddesokak"] = string.IsNullOrWhiteSpace(cari.Adres) ? " " : cari.Adres,
                ["mahalleSemtIlce"] = " ",
                ["sehir"] = " ",
                ["postaKodu"] = "",
                ["tel"] = cari.Telefon,
                ["fax"] = "",
                ["eposta"] = cari.Eposta,
                ["websitesi"] = "",
                ["iadeTable"] = Array.Empty<object>(),
                ["ozelMatrahTutari"] = "0",
                ["ozelMatrahOrani"] = 0,
                ["ozelMatrahVergiTutari"] = "0",
                ["vergiCesidi"] = " ",
                ["malHizmetTable"] = rows,
                ["tip"] = "\u0130skonto",
                ["matrah"] = Amount(fatura.AraToplam - fatura.IskontoToplam),
                ["malhizmetToplamTutari"] = Amount(fatura.AraToplam),
                ["toplamIskonto"] = Amount(fatura.IskontoToplam),
                ["hesaplanankdv"] = Amount(fatura.KdvToplam),
                ["vergilerToplami"] = Amount(fatura.KdvToplam),
                ["vergilerDahilToplamTutar"] = Amount(fatura.GenelToplam),
                ["odenecekTutar"] = Amount(fatura.GenelToplam),
                ["not"] = fatura.Aciklama ?? "",
                ["siparisNumarasi"] = "",
                ["siparisTarihi"] = "",
                ["irsaliyeNumarasi"] = "",
                ["irsaliyeTarihi"] = "",
                ["fisNo"] = "",
                ["fisTarihi"] = "",
                ["fisSaati"] = " ",
                ["fisTipi"] = " ",
                ["zRaporNo"] = "",
                ["okcSeriNo"] = ""
            };
        }

        private static string ExtractMessage(JsonDocument document)
        {
            if (document.RootElement.TryGetProperty("data", out var data))
                return ExtractText(data);

            if (document.RootElement.TryGetProperty("messages", out var messages))
                return ExtractText(messages);

            if (document.RootElement.TryGetProperty("error", out var error))
                return ExtractText(error);

            return document.RootElement.ToString();
        }

        private static string ExtractDataProperty(JsonDocument document, string propertyName)
        {
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return string.Empty;

            return data.TryGetProperty(propertyName, out var value) ? ExtractText(value) : string.Empty;
        }

        private static string ExtractText(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Array => string.Join(" ", element.EnumerateArray().Select(ExtractText)),
                JsonValueKind.Object => element.ToString(),
                JsonValueKind.Null => string.Empty,
                _ => element.ToString()
            };
        }

        private static bool IsSuccessMessage(string message)
        {
            var normalized = RemoveTurkishDiacritics(message).ToLowerInvariant();
            return normalized.Contains("basariyla", StringComparison.Ordinal) ||
                   normalized.Contains("success", StringComparison.Ordinal);
        }

        private static string RemoveTurkishDiacritics(string value)
        {
            return value
                .Replace('\u0131', 'i')
                .Replace('\u0130', 'I')
                .Replace('\u015f', 's')
                .Replace('\u015e', 'S')
                .Replace('\u011f', 'g')
                .Replace('\u011e', 'G')
                .Replace('\u00fc', 'u')
                .Replace('\u00dc', 'U')
                .Replace('\u00f6', 'o')
                .Replace('\u00d6', 'O')
                .Replace('\u00e7', 'c')
                .Replace('\u00c7', 'C');
        }

        private static (string FirstName, string LastName) SplitName(string value)
        {
            var parts = (value ?? string.Empty)
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return ("Potansiyel", "Tuketici");

            if (parts.Length == 1)
                return (parts[0], parts[0]);

            return (string.Join(' ', parts.Take(parts.Length - 1)), parts[^1]);
        }

        private static bool IsLikelyCompany(string taxOrIdentity)
        {
            return taxOrIdentity.Trim().Length == 10;
        }

        private static string UnitCode(string value)
        {
            var normalized = RemoveTurkishDiacritics((value ?? string.Empty).Trim()).ToLowerInvariant();
            return normalized switch
            {
                "kg" or "kilogram" => "KGM",
                "gr" or "gram" => "GRM",
                "lt" or "litre" or "liter" => "LTR",
                "m" or "metre" or "meter" => "MTR",
                "saat" => "HUR",
                "gun" => "DAY",
                "ay" => "MON",
                _ => "C62"
            };
        }

        private static string Amount(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static Uri GetLoginUri(bool testModu)
        {
            return new Uri(GetBaseUri(testModu) + "earsiv-services/assos-login");
        }

        private static Uri GetDispatchUri(bool testModu)
        {
            return new Uri(GetBaseUri(testModu) + "earsiv-services/dispatch");
        }

        private static string GetBaseUri(bool testModu)
        {
            return testModu
                ? "https://earsivportaltest.efatura.gov.tr/"
                : "https://earsivportal.efatura.gov.tr/";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "GİB Portal bağlantısı başarısız." : value.Trim();
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
