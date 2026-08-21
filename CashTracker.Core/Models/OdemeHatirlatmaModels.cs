namespace CashTracker.Core.Models;

using System.Globalization;

public sealed record OdemeHatirlatmaIcerigi(
    int IsletmeId,
    string IsletmeAdi,
    string AliciEposta,
    string CariUnvan,
    string FaturaNo,
    DateTime FaturaTarihi,
    DateTime VadeTarihi,
    decimal KalanTutar,
    string ParaBirimi);

public sealed record OdemeHatirlatmaOnizleme(
    int FaturaId,
    string IsletmeAdi,
    string AliciEposta,
    string CariUnvan,
    string FaturaNo,
    DateTime FaturaTarihi,
    DateTime? VadeTarihi,
    decimal KalanTutar,
    string ParaBirimi,
    string Konu,
    string Mesaj,
    bool Gonderilebilir,
    string Engel,
    DateTime? SonGonderimAt);

public sealed record OdemeHatirlatmaGonderimSonucu(
    bool Gonderildi,
    string Mesaj,
    DateTime? GonderildiAt);

public static class OdemeHatirlatmaMetni
{
    public static string BuildSubject(OdemeHatirlatmaIcerigi reminder)
    {
        return $"{reminder.IsletmeAdi} ödeme hatırlatması | {reminder.FaturaNo}";
    }

    public static string BuildMessage(OdemeHatirlatmaIcerigi reminder)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var amount = reminder.KalanTutar.ToString("N2", culture);
        var currency = reminder.ParaBirimi switch
        {
            "TRY" => "TL",
            "USD" => "USD",
            "EUR" => "EUR",
            _ => reminder.ParaBirimi
        };

        return $"""
               Merhaba {reminder.CariUnvan},

               {reminder.IsletmeAdi} tarafından düzenlenen {reminder.FaturaNo} numaralı faturanıza ait {amount} {currency} kalan ödemenin vade tarihi {reminder.VadeTarihi:dd.MM.yyyy}.

               Ödemenizi yaptıysanız bu mesajı dikkate almayabilirsiniz.

               Bu hatırlatma {reminder.IsletmeAdi} adına Systemcel üzerinden gönderildi.

               Systemcel ile gönderildi · systemcel.app
               """;
    }
}
