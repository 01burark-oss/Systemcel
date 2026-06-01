using System.Collections.Generic;

namespace CashTracker.Infrastructure.Services
{
    public static class TelegramCommandCatalog
    {
        public static IReadOnlyList<TelegramBotCommand> BotMenu { get; } = new[]
        {
            new TelegramBotCommand("yardim", "Komut listesi ve kullanım notları"),
            new TelegramBotCommand("bugun", "Bugünün gelir/gider raporu"),
            new TelegramBotCommand("ozet", "Son N gün özetini gösterir"),
            new TelegramBotCommand("rapor", "TXT rapor dosyası oluşturur"),
            new TelegramBotCommand("gelir", "Hızlı gelir kaydı ekler"),
            new TelegramBotCommand("gider", "Hızlı gider kaydı ekler"),
            new TelegramBotCommand("stok", "Barkodla stok hareketi başlatır"),
            new TelegramBotCommand("yedek", "Veritabanı yedeği gönderir")
        };

        public static string BuildHelpText(string businessName)
        {
            return
                "Systemcel Telegram Asistanı\n" +
                $"İşletme: {businessName}\n" +
                "\n" +
                "En çok kullanılan komutlar:\n" +
                "/bugun - Bugünün raporu\n" +
                "/ozet [gün] - Son N gün özeti (varsayılan 30)\n" +
                "/rapor [gün] - TXT rapor dosyası (varsayılan 30)\n" +
                "/gelir <tutar> [kalem] [açıklama]\n" +
                "/gider <tutar> <kalem> [açıklama]\n" +
                "/stok <barkod> +10 veya /stok <barkod> -3\n" +
                "/yedek - Veritabanı yedeği gönder\n" +
                "\n" +
                "Fiş OCR:\n" +
                "- Bota fiş fotoğrafı gönder\n" +
                "- Eksik eşleşmede sıra numarası, kalem adı, `yeni: <ad>`, `atla` veya `iptal` yaz\n" +
                "- Son adımda `onayla` ile kaydet\n" +
                "\n" +
                "Onay komutları:\n" +
                "/onay <kod> - Silme onayı verir\n" +
                "/iptal <kod> - İşlemi reddeder veya aktif oturumu iptal eder";
        }
    }
}
