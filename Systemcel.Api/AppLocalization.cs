using System;
using System.Collections.Generic;
using System.Globalization;

namespace Systemcel.Api
{
    internal sealed class LanguageOption
    {
        public string Code { get; init; } = "tr";
        public string DisplayName { get; init; } = string.Empty;
    }

    internal static class AppLocalization
    {
        private const string DefaultLanguage = "tr";
        private static string _currentLanguage = DefaultLanguage;

        private sealed class Entry
        {
            public Entry(string tr, string en, string de)
            {
                Tr = tr;
                En = en;
                De = de;
            }

            public string Tr { get; }
            public string En { get; }
            public string De { get; }
        }

        private static readonly Dictionary<string, Entry> Texts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tip.income"] = new("Gelir", "Income", "Einnahme"),
            ["tip.expense"] = new("Gider", "Expense", "Ausgabe"),
            ["common.date"] = new("Tarih", "Date", "Datum"),
            ["common.type"] = new("Tip", "Type", "Typ"),
            ["common.method"] = new("Yöntem", "Method", "Methode"),
            ["common.amount"] = new("Tutar", "Amount", "Betrag"),
            ["common.category"] = new("Kalem", "Category", "Kategorie"),
            ["common.description"] = new("Açıklama", "Description", "Beschreibung"),
            ["common.save"] = new("Kaydet", "Save", "Speichern"),
            ["common.new"] = new("Yeni", "New", "Neu"),
            ["common.delete"] = new("Sil", "Delete", "Loeschen"),
            ["common.refresh"] = new("Yenile", "Refresh", "Aktualisieren"),
            ["common.unknown"] = new("Bilinmiyor", "Unknown", "Unbekannt"),
            ["payment.cash"] = new("Nakit", "Cash", "Bar"),
            ["payment.card"] = new("Kredi kartı", "Credit Card", "Kreditkarte"),
            ["payment.online"] = new("Online ödeme", "Online Payment", "Online-Zahlung"),
            ["payment.transfer"] = new("Havale", "Bank Transfer", "Ueberweisung"),

            ["main.title"] = new("Systemcel gösterge paneli", "Systemcel Dashboard", "Systemcel Dashboard"),
            ["main.menu"] = new("MENU", "MENU", "MENU"),
            ["main.nav.records"] = new("Gelir ve gider kayıtları", "Income / Expense Records", "Einnahmen-/Ausgabenbuchungen"),
            ["main.nav.settings"] = new("Ayarlar", "Settings", "Einstellungen"),
            ["main.nav.print"] = new("Yazdır", "Print", "Drucken"),
            ["main.nav.short.records"] = new("KG", "REC", "BUC"),
            ["main.nav.short.settings"] = new("AYR", "SET", "EIN"),
            ["main.nav.short.print"] = new("YZD", "PRN", "DRK"),
            ["main.top.commandPanel"] = new("Komut Paneli", "Command Panel", "Befehlsbereich"),
            ["main.footer.credit"] = new("Made by Burak \u00d6zmen", "Made by Burak \u00d6zmen", "Made by Burak \u00d6zmen"),
            ["main.badge.telegramActive"] = new("Telegram aktif", "Telegram Active", "Telegram Aktiv"),
            ["main.badge.telegramInactive"] = new("Telegram pasif", "Telegram Inactive", "Telegram Inaktiv"),
            ["main.badge.localData"] = new("Yerel veri", "Local Data", "Lokale Daten"),
            ["main.badge.updateAvailable"] = new("Güncelleme hazır", "Update Available", "Update Verfuegbar"),
            ["main.badge.updateRequired"] = new("Zorunlu güncelleme", "Mandatory Update", "Pflicht-Update"),
            ["license.banner.title"] = new("Lisans gerekli", "License required", "Lizenz erforderlich"),
            ["license.banner.body"] = new("Lisans girilmedi.", "License was not entered.", "Lizenz wurde nicht eingegeben."),
            ["license.banner.remaining"] = new("Lisans girilmedi. {0} günlük kullanım hakkınız kaldı. Lisans anahtarını Ayarlar ekranından girebilirsiniz.", "License not entered. {0} days of use remain. Enter the license key from Settings.", "Lizenz nicht eingegeben. {0} Nutzungstage verbleiben. Geben Sie den Lizenzschluessel in den Einstellungen ein."),
            ["license.banner.action"] = new("Lisansı gir", "Enter License", "Lizenz Eingeben"),
            ["main.section.snapshotTitle"] = new("Finans özeti", "Financial Snapshot", "Finanzueberblick"),
            ["main.section.snapshotSubtitle"] = new("Kritik KPI kartları ve rapor panelleri", "Critical KPI cards and report panels", "Kritische KPI-Karten und Berichtspanels"),
            ["main.activeBusiness"] = new("Raporların aktif işletmesi: {0}", "Reports Active Business: {0}", "Berichte Aktives Unternehmen: {0}"),
            ["main.daily.cardTitle"] = new("Gün özeti", "Daily Overview", "Tagesueberblick"),
            ["main.daily.subtitle"] = new("Ödeme yöntemine göre dağılım", "Breakdown by Payment Method", "Verteilung nach Zahlungsmethode"),
            ["main.daily.totalIncome"] = new("Toplam Gelir", "Total Income", "Gesamteinnahmen"),
            ["main.daily.totalExpense"] = new("Toplam Gider", "Total Expense", "Gesamtausgaben"),
            ["main.daily.net"] = new("Net kâr", "Net Profit", "Nettogewinn"),
            ["main.summary.daily"] = new("Günlük", "Daily", "Taeglich"),
            ["main.summary.last30"] = new("Son 30 gün", "Last 30 Days", "Letzte 30 Tage"),
            ["main.summary.last365"] = new("Son 365 gün", "Last 365 Days", "Letzte 365 Tage"),
            ["main.summary.range.daily"] = new("Günlük", "Daily", "Taeglich"),
            ["main.summary.range.weekly"] = new("Haftalık", "Weekly", "Woechentlich"),
            ["main.summary.range.last30Days"] = new("Son 30 gün", "Last 30 Days", "Letzte 30 Tage"),
            ["main.summary.range.monthly"] = new("Aylık", "Monthly", "Monatlich"),
            ["main.summary.range.currentMonth"] = new("{0} Ayı", "{0}", "{0}"),
            ["main.summary.range.last3Months"] = new("Son 3 aylık", "Last 3 Months", "Letzte 3 Monate"),
            ["main.summary.range.last6Months"] = new("Son 6 aylık", "Last 6 Months", "Letzte 6 Monate"),
            ["main.summary.range.last1Year"] = new("Son 1 yıl", "Last 1 Year", "Letztes 1 Jahr"),
            ["main.summary.sendTelegram"] = new("Telegram'a gönder", "Send to Telegram", "An Telegram senden"),
            ["main.period.monthlyTitle"] = new("Aylık finans", "Monthly Finance", "Monatliche Finanzen"),
            ["main.period.monthlySubtitle"] = new("Seçili ay özeti", "Summary of selected month", "Zusammenfassung des ausgewaehlten Monats"),
            ["main.period.monthlySend"] = new("Aylığı gönder", "Send Monthly", "Monatlich senden"),
            ["main.period.yearlyTitle"] = new("Yıllık finans", "Yearly Finance", "Jaehrliche Finanzen"),
            ["main.period.yearlySubtitle"] = new("Seçili yıl özeti", "Summary of selected year", "Zusammenfassung des ausgewaehlten Jahres"),
            ["main.period.yearlySend"] = new("Yıllığı gönder", "Send Yearly", "Jaehrlich senden"),
            ["main.summary.income"] = new("Gelir: {0:n2}", "Income: {0:n2}", "Einnahme: {0:n2}"),
            ["main.summary.expense"] = new("Gider: {0:n2}", "Expense: {0:n2}", "Ausgabe: {0:n2}"),
            ["main.summary.net"] = new("Net: {0:n2}", "Net: {0:n2}", "Netto: {0:n2}"),
            ["main.shortcut.promptBody"] = new("Yeni sürüm açıldı. Masaüstüne kısayol oluşturmak ister misiniz?", "A new version opened. Create a desktop shortcut?", "Eine neue Version wurde gestartet. Desktop-Verknuepfung erstellen?"),
            ["main.shortcut.promptTitle"] = new("Masaüstü Kısayolu", "Desktop Shortcut", "Desktop-Verknuepfung"),
            ["main.shortcut.errorBody"] = new("Kısayol oluşturulamadı. Masaüstü erişim izinlerini kontrol edebilirsiniz.", "Shortcut could not be created. Check desktop access permissions.", "Verknuepfung konnte nicht erstellt werden. Pruefen Sie die Desktop-Berechtigung."),
            ["main.shortcut.errorTitle"] = new("Kısayol Oluşturma", "Create Shortcut", "Verknuepfung Erstellen"),
            ["main.telegram.dailyTitle"] = new("Günlük özet", "Daily Summary", "Tageszusammenfassung"),
            ["main.telegram.last30Title"] = new("Son 30 gün özeti", "Last 30 Days Summary", "Zusammenfassung Letzte 30 Tage"),
            ["main.telegram.last365Title"] = new("Son 365 gün özeti", "Last 365 Days Summary", "Zusammenfassung Letzte 365 Tage"),
            ["main.telegram.dynamicTitle"] = new("{0} özeti", "{0} Summary", "{0} Zusammenfassung"),
            ["main.telegram.monthlyTitle"] = new("Aylık özet ({0})", "Monthly Summary ({0})", "Monatszusammenfassung ({0})"),
            ["main.telegram.yearlyTitle"] = new("Yıllık özet ({0})", "Yearly Summary ({0})", "Jahreszusammenfassung ({0})"),
            ["main.telegram.notConfigured"] = new("Telegram ayarları eksik. Ayarlar > Telegram adımını kullan.", "Telegram settings are missing. Use Settings > Telegram.", "Telegram-Einstellungen fehlen. Nutzen Sie Einstellungen > Telegram."),
            ["main.telegram.range"] = new("Aralık: {0:yyyy-MM-dd} - {1:yyyy-MM-dd}", "Range: {0:yyyy-MM-dd} - {1:yyyy-MM-dd}", "Zeitraum: {0:yyyy-MM-dd} - {1:yyyy-MM-dd}"),
            ["main.telegram.business"] = new("İşletme: {0}", "Business: {0}", "Unternehmen: {0}"),
            ["main.telegram.tx"] = new("İşlem: {0} (Gelir {1}, Gider {2})", "Transactions: {0} (Income {1}, Expense {2})", "Buchungen: {0} (Einnahmen {1}, Ausgaben {2})"),
            ["main.telegram.sent"] = new("Özet Telegram'a gönderildi.", "Summary sent to Telegram.", "Zusammenfassung an Telegram gesendet."),
            ["main.telegram.sendError"] = new("Telegram gönderim hatası: {0}", "Telegram send error: {0}", "Telegram-Sendefehler: {0}"),
            ["main.telegram.categoryHeader"] = new("{0} Kalemleri:", "{0} Categories:", "{0}-Kategorien:"),
            ["main.telegram.noRecord"] = new("- Kayıt yok.", "- No records.", "- Keine Eintraege."),
            ["main.telegram.categoryRow"] = new("- {0}: {1:n2} ({2} işlem)", "- {0}: {1:n2} ({2} tx)", "- {0}: {1:n2} ({2} Buchungen)"),
            ["main.telegram.methodsHeader"] = new("Ödeme Yöntemleri:", "Payment Methods:", "Zahlungsmethoden:"),
            ["main.telegram.methodRow"] = new("- {0}: Gelir {1:n2} | Gider {2:n2} | Net {3:n2}", "- {0}: Income {1:n2} | Expense {2:n2} | Net {3:n2}", "- {0}: Einnahmen {1:n2} | Ausgaben {2:n2} | Netto {3:n2}"),
            ["main.telegram.defaultExpenseCategory"] = new("Genel Gider", "General Expense", "Allgemeine Ausgabe"),
            ["main.telegram.defaultIncomeCategory"] = new("Genel Gelir", "General Income", "Allgemeine Einnahme"),
            ["main.bot.savedBody"] = new("Bot ayarları kaydedildi. Değişiklikleri uygulamak için uygulama yeniden başlatılacak.", "Bot settings were saved. The app will restart to apply changes.", "Bot-Einstellungen gespeichert. Die App wird neu gestartet, um Aenderungen anzuwenden."),
            ["main.bot.savedTitle"] = new("Ayarlar Kaydedildi", "Settings Saved", "Einstellungen Gespeichert"),
            ["main.update.missingConfigBody"] = new("Güncelleme ayarları hazır değil. Uygulama yöneticinizden güncelleme ayarlarını tamamlamasını isteyin.", "Update settings are missing. Add Update:RepoOwner and Update:RepoName to appsettings.json.", "Update-Einstellungen fehlen. Fuegen Sie Update:RepoOwner und Update:RepoName in appsettings.json ein."),
            ["main.update.missingConfigTitle"] = new("Güncelleme ayarı eksik", "Update Settings Missing", "Update-Einstellungen Fehlen"),
            ["main.update.checkingButton"] = new("Denetleniyor, lütfen bekleyin", "Checking, please wait", "Pruefung laeuft, bitte warten"),
            ["main.update.upToDateBody"] = new("Uygulama güncel.\nSürüm: {0}", "Application is up to date.\nVersion: {0}", "Anwendung ist aktuell.\nVersion: {0}"),
            ["main.update.upToDateTitle"] = new("Güncelleme", "Update", "Update"),
            ["main.update.assetMissingBody"] = new("Yeni sürüm bulundu: {0}\nİndirme dosyası bulunamadı. İndirme sayfası açılsın mı?", "New version found: {0}\nBut no downloadable asset was found. Open the release page?", "Neue Version gefunden: {0}\nAber kein herunterladbares Asset gefunden. Release-Seite oeffnen?"),
            ["main.update.availableTitle"] = new("Güncelleme hazır", "Update Available", "Update Verfuegbar"),
            ["main.update.confirmDownloadBody"] = new("Yeni sürüm bulundu: {0}\n\nGüncelleme paketi indirilsin mi?", "New version found: {0}\n\nDownload update package?", "Neue Version gefunden: {0}\n\nUpdate-Paket herunterladen?"),
            ["main.update.checksumMissingBody"] = new("Güncellemenin güvenlik doğrulaması bulunamadı. Güncelleme iptal edildi.", "Secure checksum file (.sha256) was not found. Update was cancelled.", "Sichere Pruefsummen-Datei (.sha256) wurde nicht gefunden. Update wurde abgebrochen."),
            ["main.update.checksumMissingTitle"] = new("Güncelleme Doğrulanamadı", "Update Not Verified", "Update Nicht Verifiziert"),
            ["main.update.startedBody"] = new("Güncelleme kurulumu başlatıldı. Uygulama kapatılacak.", "Update installation started. The app will close.", "Die Update-Installation wurde gestartet. Die App wird geschlossen."),
            ["main.update.startedTitle"] = new("Güncelleme başlatıldı", "Update Started", "Update Gestartet"),
            ["main.update.errorBody"] = new("Güncelleme denetleme hatası: {0}", "Update check error: {0}", "Fehler bei der Update-Pruefung: {0}"),
            ["main.update.errorTitle"] = new("Güncelleme Hatası", "Update Error", "Update-Fehler"),
            ["main.update.packageMissing"] = new("Güncelleme paketi bulunamadı.", "Update package was not found.", "Update-Paket wurde nicht gefunden."),
            ["main.update.assetMissingInZip"] = new("Güncelleme dosyasında gerekli içerik bulunamadı. Dağıtım paketini kontrol edin.", "'{0}' was not found in the update package. Check release asset contents.", "'{0}' wurde im Update-Paket nicht gefunden. Pruefen Sie den Asset-Inhalt."),
            ["main.update.completedBodyWithDesktop"] = new("Güncelleme yüklendi. Yeni sürüm başlatıldı.\nMasaüstü uygulaması güncellenecek.", "Update installed. New version started.\nDesktop exe will be updated.", "Update installiert. Neue Version gestartet.\nDesktop-exe wird aktualisiert."),
            ["main.update.completedBody"] = new("Güncelleme yüklendi. Yeni sürüm başlatıldı.", "Update installed. New version started.", "Update installiert. Neue Version gestartet."),
            ["main.update.completedTitle"] = new("Güncelleme Tamamlandı", "Update Completed", "Update Abgeschlossen"),
            ["main.update.invalidChecksumFile"] = new("Güncelleme doğrulama bilgisi geçersiz veya eksik.", "Checksum file is invalid or target file hash was not found.", "Pruefsummen-Datei ist ungueltig oder der Datei-Hash wurde nicht gefunden."),
            ["main.update.checksumMismatch"] = new("İndirilen güncelleme doğrulanamadı. Güvenlik nedeniyle işlem durduruldu.", "Downloaded file checksum validation failed. Update was stopped for security.", "Die Pruefsummenpruefung der heruntergeladenen Datei ist fehlgeschlagen. Update wurde aus Sicherheitsgruenden gestoppt."),

            ["print.title"] = new("Yazdır Önizleme", "Print Preview", "Druckvorschau"),
            ["print.panel.title"] = new("Rapor Kontrolleri", "Report Controls", "Berichtssteuerung"),
            ["print.panel.subtitle"] = new("Şablonu ve tarih aralığını seçin.", "Manage template, date range and output actions here.", "Verwalten Sie hier Vorlage, Datumsbereich und Ausgaben."),
            ["print.panel.actions"] = new("Çıktı İşlemleri", "Output Actions", "Ausgabeaktionen"),
            ["print.panel.hint"] = new("Önizleme, seçimler değiştikçe güncellenir.", "Note: preview refreshes automatically when selections change.", "Hinweis: Die Vorschau wird bei Aenderungen automatisch aktualisiert."),
            ["print.label.template"] = new("Şablon", "Template", "Vorlage"),
            ["print.label.range"] = new("Tarih Aralığı", "Date Range", "Datumsbereich"),
            ["print.label.from"] = new("Başlangıç", "From", "Von"),
            ["print.label.to"] = new("Bitiş", "To", "Bis"),
            ["print.label.note"] = new("Not / Açıklama", "Note / Comment", "Notiz / Kommentar"),
            ["print.template.executive"] = new("Yönetici Özeti", "Executive Summary", "Management Summary"),
            ["print.template.accounting"] = new("Muhasebe Raporu", "Accounting Report", "Buchhaltungsbericht"),
            ["print.range.custom"] = new("Özel Tarih Aralığı", "Custom Date Range", "Benutzerdefinierter Bereich"),
            ["print.range.between"] = new("{0:dd.MM.yyyy} - {1:dd.MM.yyyy}", "{0:MM/dd/yyyy} - {1:MM/dd/yyyy}", "{0:dd.MM.yyyy} - {1:dd.MM.yyyy}"),
            ["print.range.named"] = new("{0} ({1:dd.MM.yyyy} - {2:dd.MM.yyyy})", "{0} ({1:MM/dd/yyyy} - {2:MM/dd/yyyy})", "{0} ({1:dd.MM.yyyy} - {2:dd.MM.yyyy})"),
            ["print.button.print"] = new("Yazdır", "Print", "Drucken"),
            ["print.button.savePdf"] = new("PDF Olarak Kaydet", "Save as PDF", "Als PDF Speichern"),
            ["print.button.export"] = new("Dışa Aktar (.html)", "Export (.html)", "Exportieren (.html)"),
            ["print.button.applyNote"] = new("Notu Kaydet", "Save Note", "Notiz Speichern"),
            ["print.status.loading"] = new("Önizleme güncelleniyor...", "Refreshing preview...", "Vorschau wird aktualisiert..."),
            ["print.status.ready"] = new("Önizleme hazır. Kayıt örneği: {0}", "Preview ready. Sample rows: {0}", "Vorschau bereit. Beispielzeilen: {0}"),
            ["print.status.error"] = new("Önizleme oluşturulamadı: {0}", "Preview could not be created: {0}", "Vorschau konnte nicht erstellt werden: {0}"),
            ["print.validation.template"] = new("Lütfen bir şablon seçin.", "Please select a template.", "Bitte waehlen Sie eine Vorlage aus."),
            ["print.validation.dateRange"] = new("Başlangıç tarihi bitiş tarihinden sonra olamaz.", "Start date cannot be after end date.", "Das Startdatum darf nicht nach dem Enddatum liegen."),
            ["print.meta.range"] = new("Aralık", "Range", "Bereich"),
            ["print.meta.generatedAt"] = new("Oluşturulma", "Generated", "Erstellt"),
            ["print.meta.code"] = new("Belge No", "Document No", "Dok.-Nr."),
            ["print.meta.template"] = new("Şablon", "Template", "Vorlage"),
            ["print.meta.source"] = new("Kaynak", "Source", "Quelle"),
            ["print.section.summary"] = new("Özet Finansal Görünüm", "Summary Financial View", "Finanzuebersicht"),
            ["print.section.paymentMethods"] = new("Ödeme Yöntemi Dağılımı", "Payment Method Distribution", "Verteilung nach Zahlungsmethode"),
            ["print.section.paymentMethodsCompact"] = new("Ay Sonu Özet Raporu (Ödeme Yöntemine Göre)", "Period Summary Report (By Payment Method)", "Periodenbericht (Nach Zahlungsmethode)"),
            ["print.section.paymentMethodsShort"] = new("Ödeme Yöntemleri", "Payment Methods", "Zahlungsarten"),
            ["print.section.note"] = new("Yönetici Notu", "Management Note", "Management-Notiz"),
            ["print.section.incomeCategories"] = new("Gelir Kalem Toplamları", "Income Category Totals", "Einnahmekategorien"),
            ["print.section.incomeCategoriesShort"] = new("Gelir Kalemleri", "Income Categories", "Einnahmekategorien"),
            ["print.section.expenseCategories"] = new("Gider Kalem Toplamları", "Expense Category Totals", "Ausgabenkategorien"),
            ["print.section.expenseCategoriesShort"] = new("Gider Kalemleri", "Expense Categories", "Ausgabenkategorien"),
            ["print.section.records"] = new("Kayıt Listesi", "Record List", "Buchungsliste"),
            ["print.summary.incomeShort"] = new("Gelir", "Income", "Einnahmen"),
            ["print.summary.expenseShort"] = new("Gider", "Expense", "Ausgaben"),
            ["print.summary.totalIncome"] = new("Toplam Gelir (₺)", "Total Income", "Gesamteinnahmen"),
            ["print.summary.totalExpense"] = new("Toplam Gider (₺)", "Total Expense", "Gesamtausgaben"),
            ["print.metric.count"] = new("İşlem adedi: {0}", "Transaction count: {0}", "Buchungen: {0}"),
            ["print.metric.totalCount"] = new("Toplam işlem: {0}", "Total transactions: {0}", "Gesamtbuchungen: {0}"),
            ["print.note.placeholder"] = new("Bu rapor için not girilmedi.", "No note was entered for this report.", "Fuer diesen Bericht wurde keine Notiz eingegeben."),
            ["print.preview.sampleNote"] = new("Önizleme yalnızca ilk {0} kaydı gösterir. Toplam kayıt: {1}.", "Preview shows only the first {0} records. Total records: {1}.", "Die Vorschau zeigt nur die ersten {0} Eintraege. Gesamt: {1}."),
            ["print.column.count"] = new("Adet", "Count", "Anzahl"),
            ["print.total.label"] = new("TOPLAM", "TOTAL", "GESAMT"),
            ["print.total.general"] = new("GENEL TOPLAM", "GRAND TOTAL", "GESAMTSUMME"),
            ["print.headline.suffix"] = new("GELİR-GİDER TAKİP TABLOSU", "INCOME-EXPENSE TRACKING TABLE", "EINNAHMEN-AUSGABEN UEBERSICHT"),
            ["print.footer.page"] = new("Sayfa {0}", "Page {0}", "Seite {0}"),
            ["print.scope.title"] = new("Kayıt kapsamı", "Record Scope", "Buchungsumfang"),
            ["print.scope.header"] = new("Yazdırma kapsamını seç", "Select print scope", "Druckumfang waehlen"),
            ["print.scope.body"] = new("Muhasebe raporunda kayıt listesi yer alıyor. Yazdırma için hangi kayıt kapsamıyla devam edilsin?", "The accounting report includes the record list. Which record scope should be used for printing?", "Der Buchhaltungsbericht enthaelt eine Buchungsliste. Welcher Umfang soll fuer den Druck verwendet werden?"),
            ["print.scope.first50"] = new("İlk 50", "First 50", "Erste 50"),
            ["print.scope.first100"] = new("İlk 100", "First 100", "Erste 100"),
            ["print.scope.all"] = new("Tümü", "All", "Alle"),
            ["print.scope.cancel"] = new("Vazgeç", "Cancel", "Abbrechen"),
            ["print.error.pdfPrinterMissing"] = new("\"Microsoft Print to PDF\" yazıcısı bulunamadı.", "\"Microsoft Print to PDF\" printer was not found.", "\"Microsoft Print to PDF\"-Drucker wurde nicht gefunden."),
            ["print.export.saved"] = new("Dosya kaydedildi:\n{0}", "File saved:\n{0}", "Datei gespeichert:\n{0}"),

            ["kasa.title"] = new("Gelir / Gider", "Income / Expense", "Einnahmen / Ausgaben"),
            ["kasa.titleWithBusiness"] = new("Gelir / Gider - {0}", "Income / Expense - {0}", "Einnahmen / Ausgaben - {0}"),
            ["kasa.activeBusiness"] = new("Aktif İşletme: {0}", "Active Business: {0}", "Aktives Unternehmen: {0}"),
            ["kasa.records.title"] = new("Kayıtlar", "Records", "Buchungen"),
            ["kasa.records.subtitle"] = new("Tüm hareketler ve ayrıntılı liste", "All transactions and detail list", "Alle Vorgaenge und Detailansicht"),
            ["kasa.form.title"] = new("İşlem Formu", "Transaction Form", "Transaktionsformular"),
            ["kasa.form.subtitle"] = new("Kayıt düzenleme ve işlem komutları", "Record editing and action commands", "Buchungsbearbeitung und Aktionsbefehle"),
            ["kasa.manageCategories"] = new("Kalemleri Yönet", "Manage Categories", "Kategorien Verwalten"),
            ["kasa.amount.placeholder"] = new("tutar girin", "enter amount", "betrag eingeben"),
            ["kasa.error.categoryRequired"] = new("Gelir veya gider kalemi zorunludur. Ayarlar ekranından kalem ekleyebilirsin.", "Income/expense category is required. You can add one in Settings.", "Einnahmen-/Ausgabenkategorie ist erforderlich. Sie koennen sie in den Einstellungen hinzufuegen."),
            ["kasa.error.amountRequired"] = new("Tutar zorunludur.", "Amount is required.", "Betrag ist erforderlich."),
            ["kasa.error.amountInvalid"] = new("Geçerli bir tutar girin.", "Enter a valid amount.", "Geben Sie einen gueltigen Betrag ein."),
            ["kasa.error.amountPositive"] = new("Tutar 0'dan büyük olmalıdır.", "Amount must be greater than 0.", "Der Betrag muss groesser als 0 sein."),
            ["kasa.delete.confirmBody"] = new("Kaydı silmek istiyor musun?", "Do you want to delete this record?", "Moechten Sie diesen Eintrag loeschen?"),
            ["kasa.delete.confirmTitle"] = new("Onay", "Confirmation", "Bestaetigung"),
            ["kasa.error.categoryLoad"] = new("Kalemler yüklenirken hata oluştu: {0}", "An error occurred while loading categories: {0}", "Beim Laden der Kategorien ist ein Fehler aufgetreten: {0}"),
            ["kasa.hint.noCategory"] = new("{0} için kalem tanımı bulunamadı. Önce Ayarlar ekranından bir kalem eklemelisin.", "No category is defined for {0}. Add one first in Settings.", "Fuer {0} ist keine Kategorie definiert. Fuegen Sie zuerst eine in den Einstellungen hinzu."),

            ["settings.title"] = new("Ayarlar", "Settings", "Einstellungen"),
            ["settings.business.title"] = new("İşletme ayarları", "Business Settings", "Unternehmenseinstellungen"),
            ["settings.business.subtitle"] = new("Aktif işletmeyi seç, adını güncelle ve yeni işletme ekle.", "Select active business, rename it, and add a new one.", "Aktives Unternehmen auswaehlen, umbenennen und neues Unternehmen hinzufuegen."),
            ["settings.category.title"] = new("Gelir / Gider Kalemleri", "Income / Expense Categories", "Einnahmen-/Ausgabenkategorien"),
            ["settings.category.subtitle"] = new("Seçili işletme için kalemleri yönet.", "Manage categories for selected business.", "Kategorien fuer das ausgewaehlte Unternehmen verwalten."),
            ["settings.label.activeBusiness"] = new("Aktif İşletme", "Active Business", "Aktives Unternehmen"),
            ["settings.label.businessName"] = new("İşletme Adı", "Business Name", "Unternehmensname"),
            ["settings.label.newBusiness"] = new("Yeni İşletme", "New Business", "Neues Unternehmen"),
            ["settings.label.language"] = new("Uygulama Dili", "Application Language", "Anwendungssprache"),
            ["settings.button.applyLanguage"] = new("Dili Uygula", "Apply Language", "Sprache Anwenden"),
            ["settings.button.setActive"] = new("Aktif Yap", "Set Active", "Aktiv Setzen"),
            ["settings.button.renameBusiness"] = new("Yeniden Adlandir", "Rename", "Umbenennen"),
            ["settings.button.deleteBusiness"] = new("İşletmeyi Sil", "Delete Business", "Unternehmen Loeschen"),
            ["settings.button.addBusiness"] = new("İşletme Ekle", "Add Business", "Unternehmen Hinzufuegen"),
            ["settings.button.updateCategory"] = new("Seçiliyi Güncelle", "Update Selected", "Auswahl Aktualisieren"),
            ["settings.button.deleteCategory"] = new("Seçili Kalemi Sil", "Delete Selected Category", "Ausgewaehlte Kategorie Loeschen"),
            ["settings.button.addCategory"] = new("Kalem Ekle", "Add Category", "Kategorie Hinzufuegen"),
            ["settings.contact"] = new("İletişim: 01burark@gmail.com", "Contact: 01burark@gmail.com", "Kontakt: 01burark@gmail.com"),
            ["settings.license.title"] = new("Lisans", "License", "Lizenz"),
            ["settings.license.key"] = new("Lisans Anahtari", "License Key", "Lizenzschluessel"),
            ["settings.license.copy"] = new("Kopyala", "Copy", "Kopieren"),
            ["settings.license.activate"] = new("Lisansi Aktive Et", "Activate License", "Lizenz Aktivieren"),
            ["settings.license.contact"] = new("Lisans ve OCR aktivasyonu için iletişim: Instagram @_6uwak | E-posta: 01burark@gmail.com", "For license and OCR activation: Instagram @_6uwak | Email: 01burark@gmail.com", "Fuer Lizenz- und OCR-Aktivierung: Instagram @_6uwak | E-Mail: 01burark@gmail.com"),
            ["settings.license.status.active"] = new("Aktif lisans: {0}", "Active license: {0}", "Aktive Lizenz: {0}"),
            ["settings.license.status.legacy"] = new("Bu kurulum lisans gecisinden muaf.", "This installation is exempt from licensing rollout.", "Diese Installation ist vom Lizenz-Rollout ausgenommen."),
            ["settings.license.status.trial"] = new("Trial aktif. Kalan süre: {0} gun.", "Trial active. Remaining time: {0} days.", "Testphase aktiv. Verbleibende Zeit: {0} Tage."),
            ["settings.status.active"] = new("Aktif", "Active", "Aktiv"),
            ["settings.language.same"] = new("Seçili dil zaten aktif.", "Selected language is already active.", "Die ausgewaehlte Sprache ist bereits aktiv."),
            ["settings.language.saved"] = new("Dil tercihi kaydedildi. Uygulama simdi yeniden başlatilsin mi?", "Language preference was saved. Restart the app now?", "Spracheinstellung gespeichert. Anwendung jetzt neu starten?"),
            ["settings.language.savedTitle"] = new("Dil Değişikligi", "Language Change", "Sprachaenderung"),
            ["settings.language.savedHint"] = new("Dil tercihi kaydedildi. Değişiklikler sonraki açılışta uygulanacak.", "Language preference was saved. Changes will apply on next launch.", "Spracheinstellung gespeichert. Aenderungen gelten beim naechsten Start."),
            ["settings.hint.businessesLoading"] = new("İşletmeler yükleniyor...", "Loading businesses...", "Unternehmen werden geladen..."),
            ["settings.hint.noBusiness"] = new("İşletme bulunamadı. Yeni işletme ekleyerek başlayabilirsin.", "No business found. Start by adding a new business.", "Kein Unternehmen gefunden. Starten Sie mit dem Hinzufuegen eines neuen Unternehmens."),
            ["settings.error.businessLoad"] = new("İşletme listesi yüklenirken hata oluştu: {0}", "An error occurred while loading businesses: {0}", "Beim Laden der Unternehmen ist ein Fehler aufgetreten: {0}"),
            ["settings.hint.businessLoadFail"] = new("İşletme listesi yüklenemedi.", "Business list could not be loaded.", "Unternehmensliste konnte nicht geladen werden."),
            ["settings.hint.businessActiveInfo"] = new("Bu işletme aktif. Özetler ve kayıtlar bu işletmeye gore listelenir.", "This business is active. Summaries and records are listed for this business.", "Dieses Unternehmen ist aktiv. Zusammenfassungen und Buchungen werden fuer dieses Unternehmen angezeigt."),
            ["settings.hint.businessInactiveInfo"] = new("Bu işletmeyi aktif yaparsan tum listeler seçili işletmeye gore filtrelenir.", "If you set this business active, all lists will be filtered by it.", "Wenn Sie dieses Unternehmen aktivieren, werden alle Listen danach gefiltert."),
            ["settings.hint.businessAlreadyActive"] = new("Seçili işletme zaten aktif.", "Selected business is already active.", "Das ausgewaehlte Unternehmen ist bereits aktiv."),
            ["settings.hint.businessActivated"] = new("Aktif işletme değiştirildi: {0}", "Active business changed: {0}", "Aktives Unternehmen geaendert: {0}"),
            ["settings.error.businessActivate"] = new("Aktif işletme değiştirilemedi: {0}", "Active business could not be changed: {0}", "Aktives Unternehmen konnte nicht geaendert werden: {0}"),
            ["settings.hint.businessActivateFail"] = new("Aktif işletme değiştirilemedi.", "Active business could not be changed.", "Aktives Unternehmen konnte nicht geaendert werden."),
            ["settings.error.businessNameRequired"] = new("İşletme adı boş bırakilamaz.", "Business name cannot be empty.", "Unternehmensname darf nicht leer sein."),
            ["settings.hint.businessNameRequired"] = new("İşletme adı boş bırakilamaz.", "Business name cannot be empty.", "Unternehmensname darf nicht leer sein."),
            ["settings.hint.businessNameMin"] = new("İşletme adı en az 2 karakter olmalıdır.", "Business name must be at least 2 characters.", "Unternehmensname muss mindestens 2 Zeichen lang sein."),
            ["settings.hint.businessNameSame"] = new("Yeni ad mevcut ad ile aynı.", "New name is the same as current name.", "Der neue Name ist identisch mit dem aktuellen Namen."),
            ["settings.hint.businessNameExists"] = new("Bu isimde bir işletme zaten var.", "A business with this name already exists.", "Ein Unternehmen mit diesem Namen existiert bereits."),
            ["settings.hint.businessRenamed"] = new("İşletme adı güncellendi.", "Business name updated.", "Unternehmensname aktualisiert."),
            ["settings.error.businessRename"] = new("İşletme adı güncellenemedi: {0}", "Business name could not be updated: {0}", "Unternehmensname konnte nicht aktualisiert werden: {0}"),
            ["settings.hint.businessRenameFail"] = new("İşletme adı güncellenemedi.", "Business name could not be updated.", "Unternehmensname konnte nicht aktualisiert werden."),
            ["settings.error.newBusinessNameRequired"] = new("Yeni işletme adı boş bırakilamaz.", "New business name cannot be empty.", "Neuer Unternehmensname darf nicht leer sein."),
            ["settings.hint.newBusinessNameRequired"] = new("Yeni işletme adı boş bırakilamaz.", "New business name cannot be empty.", "Neuer Unternehmensname darf nicht leer sein."),
            ["settings.hint.newBusinessNameMin"] = new("Yeni işletme adı en az 2 karakter olmalıdır.", "New business name must be at least 2 characters.", "Neuer Unternehmensname muss mindestens 2 Zeichen lang sein."),
            ["settings.hint.newBusinessNameExists"] = new("Bu isimde bir işletme zaten var.", "A business with this name already exists.", "Ein Unternehmen mit diesem Namen existiert bereits."),
            ["settings.hint.businessAdded"] = new("Yeni işletme eklendi ve aktif yapildi: {0}", "New business added and set active: {0}", "Neues Unternehmen hinzugefuegt und aktiviert: {0}"),
            ["settings.error.businessAdd"] = new("Yeni işletme eklenemedi: {0}", "New business could not be added: {0}", "Neues Unternehmen konnte nicht hinzugefuegt werden: {0}"),
            ["settings.hint.businessAddFail"] = new("Yeni işletme eklenemedi.", "New business could not be added.", "Neues Unternehmen konnte nicht hinzugefuegt werden."),
            ["settings.hint.minOneBusiness"] = new("En az bir işletme kalmali. Bu işletme silinemez.", "At least one business must remain. This business cannot be deleted.", "Mindestens ein Unternehmen muss bestehen bleiben. Dieses Unternehmen kann nicht geloescht werden."),
            ["settings.confirm.businessDeleteBody"] = new("'{0}' işletmesini silmek istiyor musun?", "Do you want to delete business '{0}'?", "Moechten Sie das Unternehmen '{0}' loeschen?"),
            ["settings.confirm.businessDeleteTitle"] = new("İşletme Sil", "Delete Business", "Unternehmen Loeschen"),
            ["settings.approval.businessDeleteTitle"] = new("İşletme silme", "Business deletion", "Unternehmen loeschen"),
            ["settings.hint.businessDeleted"] = new("İşletme silindi: {0}", "Business deleted: {0}", "Unternehmen geloescht: {0}"),
            ["settings.error.businessDelete"] = new("İşletme silinemedi: {0}", "Business could not be deleted: {0}", "Unternehmen konnte nicht geloescht werden: {0}"),
            ["settings.hint.businessDeleteFail"] = new("İşletme silinemedi.", "Business could not be deleted.", "Unternehmen konnte nicht geloescht werden."),
            ["settings.business.activeText"] = new("Aktif", "Active", "Aktiv"),
            ["settings.business.passiveText"] = new("Pasif", "Passive", "Passiv"),
            ["settings.business.approvalLine"] = new("İşletme: {0} ({1})", "Business: {0} ({1})", "Unternehmen: {0} ({1})"),
            ["settings.business.totalLine"] = new("Toplam işletme: {0}", "Total businesses: {0}", "Gesamtunternehmen: {0}"),
            ["settings.hint.categoriesLoading"] = new("Kalemler yükleniyor...", "Loading categories...", "Kategorien werden geladen..."),
            ["settings.hint.noCategoryForType"] = new("{0} için tanımlı kalem yok. Aşağıdan yeni kalem ekleyebilirsin.", "No category is defined for {0}. Add a new category below.", "Fuer {0} ist keine Kategorie definiert. Fuegen Sie unten eine neue Kategorie hinzu."),
            ["settings.hint.categoryCountForType"] = new("{0} için {1} kalem var.", "{1} categories found for {0}.", "{1} Kategorien fuer {0} gefunden."),
            ["settings.error.categoryLoad"] = new("Kalem listesi yüklenemedi: {0}", "Category list could not be loaded: {0}", "Kategorieliste konnte nicht geladen werden: {0}"),
            ["settings.hint.categoryLoadFail"] = new("Kalem listesi yüklenemedi.", "Category list could not be loaded.", "Kategorieliste konnte nicht geladen werden."),
            ["settings.error.categoryNameRequired"] = new("Kalem adı boş bırakilamaz.", "Category name cannot be empty.", "Kategoriename darf nicht leer sein."),
            ["settings.hint.categoryNameRequired"] = new("Kalem adı boş bırakilamaz.", "Category name cannot be empty.", "Kategoriename darf nicht leer sein."),
            ["settings.hint.categoryNameMin"] = new("Kalem adı en az 2 karakter olmalıdır.", "Category name must be at least 2 characters.", "Kategoriename muss mindestens 2 Zeichen lang sein."),
            ["settings.hint.categoryNameExists"] = new("Bu kalem zaten listede var.", "This category already exists in the list.", "Diese Kategorie ist bereits in der Liste vorhanden."),
            ["settings.hint.categoryAdded"] = new("{0} kalemi eklendi: {1}", "{0} category added: {1}", "{0}-Kategorie hinzugefuegt: {1}"),
            ["settings.error.categoryAdd"] = new("Kalem eklenemedi: {0}", "Category could not be added: {0}", "Kategorie konnte nicht hinzugefuegt werden: {0}"),
            ["settings.hint.categoryAddFail"] = new("Kalem eklenemedi.", "Category could not be added.", "Kategorie konnte nicht hinzugefuegt werden."),
            ["settings.hint.selectCategoryToEdit"] = new("Düzenlemek için listeden bir kalem seç.", "Select a category from the list to edit.", "Waehlen Sie eine Kategorie aus der Liste zum Bearbeiten."),
            ["settings.hint.categoryNameSame"] = new("Yeni ad mevcut ad ile aynı.", "New name is the same as current name.", "Der neue Name ist identisch mit dem aktuellen Namen."),
            ["settings.hint.categoryUpdated"] = new("Kalem güncellendi: {0} -> {1}", "Category updated: {0} -> {1}", "Kategorie aktualisiert: {0} -> {1}"),
            ["settings.error.categoryUpdate"] = new("Kalem güncellenemedi: {0}", "Category could not be updated: {0}", "Kategorie konnte nicht aktualisiert werden: {0}"),
            ["settings.hint.categoryUpdateFail"] = new("Kalem güncellenemedi.", "Category could not be updated.", "Kategorie konnte nicht aktualisiert werden."),
            ["settings.error.selectCategoryToDelete"] = new("Lütfen silmek için bir kalem seç.", "Please select a category to delete.", "Bitte waehlen Sie eine Kategorie zum Loeschen."),
            ["settings.hint.selectCategoryToDelete"] = new("Silmek için listeden bir kalem seç.", "Select a category from the list to delete.", "Waehlen Sie zum Loeschen eine Kategorie aus der Liste."),
            ["settings.confirm.categoryDeleteBody"] = new("'{0}' kalemini silmek istiyor musun?", "Do you want to delete category '{0}'?", "Moechten Sie die Kategorie '{0}' loeschen?"),
            ["settings.confirm.categoryDeleteTitle"] = new("Kalem Sil", "Delete Category", "Kategorie Loeschen"),
            ["settings.approval.categoryDeleteTitle"] = new("Kalem silme", "Category deletion", "Kategorie loeschen"),
            ["settings.hint.categoryDeleted"] = new("Kalem silindi: {0}", "Category deleted: {0}", "Kategorie geloescht: {0}"),
            ["settings.error.categoryDelete"] = new("Kalem silinemedi: {0}", "Category could not be deleted: {0}", "Kategorie konnte nicht geloescht werden: {0}"),
            ["settings.hint.categoryDeleteFail"] = new("Kalem silinemedi.", "Category could not be deleted.", "Kategorie konnte nicht geloescht werden."),
            ["settings.approval.categoryDetails"] = new("İşletme: {0}\nTip: {1}\nKalem: {2}", "Business: {0}\nType: {1}\nCategory: {2}", "Unternehmen: {0}\nTyp: {1}\nKategorie: {2}"),
            ["settings.approval.wait"] = new("Telegram onayı bekleniyor...", "Waiting for Telegram approval...", "Telegram-Bestaetigung wird erwartet..."),
            ["settings.approval.rejected"] = new("Telegram onayı reddedildi.", "Telegram approval was rejected.", "Telegram-Bestaetigung wurde abgelehnt."),
            ["settings.approval.timeout"] = new("Telegram onayı süresi doldu.", "Telegram approval timed out.", "Telegram-Bestaetigung ist abgelaufen."),
            ["settings.approval.notConfiguredBody"] = new("Telegram ayarları eksik veya komutlar kapalı. Silme işlemi için Telegram onayı gerekli.", "Telegram settings are missing or commands are disabled. Telegram approval is required for deletion.", "Telegram-Einstellungen fehlen oder Befehle sind deaktiviert. Zum Loeschen ist eine Telegram-Bestaetigung erforderlich."),
            ["settings.approval.notConfiguredHint"] = new("Telegram onayı alınmadı.", "Telegram approval not received.", "Telegram-Bestaetigung nicht erhalten."),
            ["settings.approval.failedBody"] = new("Telegram onayı alınamadı: {0}", "Telegram approval could not be received: {0}", "Telegram-Bestaetigung konnte nicht empfangen werden: {0}"),
            ["settings.approval.failedDefault"] = new("Bilinmeyen hata.", "Unknown error.", "Unbekannter Fehler."),
            ["settings.approval.failedHint"] = new("Telegram onayı alınamadı.", "Telegram approval could not be received.", "Telegram-Bestaetigung konnte nicht empfangen werden."),

            ["pin.title"] = new("Şifre girişi", "Password Entry", "Passworteingabe"),
            ["pin.header"] = new("Systemcel girişi", "Systemcel Login", "Systemcel Anmeldung"),
            ["pin.info"] = new("4 haneli şifrenizi girin.", "Enter your 4-digit password.", "Geben Sie Ihr 4-stelliges Passwort ein."),
            ["pin.button.login"] = new("Giriş", "Login", "Anmelden"),
            ["pin.button.exit"] = new("Çıkış", "Exit", "Beenden"),
            ["pin.button.forgot"] = new("Şifremi unuttum", "Forgot Password", "Passwort Vergessen"),
            ["pin.error.storage"] = new("Şifre altyapısı okunamadı.", "Password storage could not be read.", "Passwortspeicher konnte nicht gelesen werden."),
            ["pin.error.invalid"] = new("Şifre 4 haneli sayısal olmalıdır.", "Password must be 4 numeric digits.", "Passwort muss 4-stellig numerisch sein."),
            ["pin.error.wrong"] = new("Şifre hatalı.", "Wrong password.", "Falsches Passwort."),
            ["pin.error.verifyFail"] = new("Şifre doğrulanamadı: {0}", "Password could not be verified: {0}", "Passwort konnte nicht verifiziert werden: {0}"),
            ["pin.forgot.title"] = new("PIN Yardımı", "PIN Help", "PIN-Hilfe"),
            ["pin.forgot.telegramNotConfigured"] = new("PIN güvenlik nedeniyle paylaşılamaz.", "PIN cannot be shared for security reasons.", "PIN kann aus Sicherheitsgruenden nicht geteilt werden."),
            ["pin.forgot.confirm"] = new("Mevcut PIN güvenlik nedeniyle gösterilemez.", "The current PIN cannot be shown for security reasons.", "Die aktuelle PIN kann aus Sicherheitsgruenden nicht angezeigt werden."),
            ["pin.forgot.messageToTelegram"] = new("PIN gösterimi devre dışı.", "PIN reveal is disabled.", "PIN-Anzeige ist deaktiviert."),
            ["pin.forgot.sent"] = new("Destek için satıcı ile iletişime geçin.", "Contact the vendor for support.", "Kontaktieren Sie den Anbieter fuer Support."),
            ["pin.forgot.sendError"] = new("PIN yardımı gösterilemedi: {0}", "PIN help could not be shown: {0}", "PIN-Hilfe konnte nicht angezeigt werden: {0}"),

            ["initial.title"] = new("Systemcel - İlk kurulum", "Systemcel - Initial Setup", "Systemcel - Ersteinrichtung"),
            ["initial.brand.subtitle"] = new("Güvenli başlangıç ayarları", "Secure startup settings", "Sichere Starteinstellungen"),
            ["initial.steps.title"] = new("Kurulum adımları", "Setup Steps", "Einrichtungsschritte"),
            ["initial.steps.1"] = new("1) Telegram'da BotFather kullanıcısına git (mavi tikli).", "1) Go to BotFather on Telegram (verified account).", "1) Gehen Sie in Telegram zu BotFather (verifiziertes Konto)."),
            ["initial.steps.2"] = new("2) /newbot komutunu gir ve yeni botu oluştur.", "2) Run /newbot and create a new bot.", "2) Fuehren Sie /newbot aus und erstellen Sie einen neuen Bot."),
            ["initial.steps.3"] = new("3) Botuna isim ver ve kullanıcı adı belirle.", "3) Give your bot a name and username.", "3) Geben Sie Ihrem Bot einen Namen und Benutzernamen."),
            ["initial.steps.4"] = new("4) Verilen tokeni bu ekrandaki Bot Token alanına gir.", "4) Enter the provided token into the Bot Token field.", "4) Geben Sie das erhaltene Token in das Feld Bot-Token ein."),
            ["initial.steps.5"] = new("5) Telegram'da bota /start yaz.", "5) Send /start to your bot on Telegram.", "5) Senden Sie /start an Ihren Bot in Telegram."),
            ["initial.steps.6"] = new("6) Buradan User ID al butonuyla Chat ID bilgisini cek.", "6) Use the Chat ID fetch button here to get your Chat ID.", "6) Nutzen Sie hier die Chat-ID-Schaltflaeche, um Ihre Chat-ID abzurufen."),
            ["initial.steps.7"] = new("7) Bağlantıyi Test Et butonuna bas.", "7) Click Test Connection.", "7) Klicken Sie auf Verbindung testen."),
            ["initial.steps.8"] = new("8) Kurulumu Tamamla butonuna bas.", "8) Click Complete Setup.", "8) Klicken Sie auf Einrichtung abschliessen."),
            ["initial.button.back"] = new("Geri Dön", "Back", "Zurueck"),
            ["initial.card.title"] = new("Telegram Bağlantı Bilgileri", "Telegram Connection Details", "Telegram-Verbindungsdaten"),
            ["initial.card.meta"] = new("Canlı doğrulama yapılır, ardından test mesajı gönderilir.", "Live validation is performed, then a test message is sent.", "Es wird live validiert und danach eine Testnachricht gesendet."),
            ["initial.label.botToken"] = new("Bot Token", "Bot Token", "Bot-Token"),
            ["initial.label.userId"] = new("Telegram User ID (Chat ID)", "Telegram User ID (Chat ID)", "Telegram-Benutzer-ID (Chat-ID)"),
            ["initial.button.fetchChatId"] = new("Chat ID'yi Otomatik Al", "Fetch Chat ID Automatically", "Chat-ID Automatisch Abrufen"),
            ["initial.button.openBotFather"] = new("BotFather'a Git", "Open BotFather", "BotFather Oeffnen"),
            ["initial.error.botFatherOpenBody"] = new("BotFather bağlantısi açılamadı: {0}", "Could not open BotFather link: {0}", "BotFather-Link konnte nicht geoeffnet werden: {0}"),
            ["initial.error.botFatherOpenTitle"] = new("Bağlantı Hatası", "Connection Error", "Verbindungsfehler"),
            ["initial.hint.fetchChatId"] = new("Not: Bota /start yazın ve sonra Chat ID'yi Otomatik Al butonunu kullanin.", "Note: Send /start to your bot, then use Fetch Chat ID Automatically.", "Hinweis: Senden Sie /start an Ihren Bot und nutzen Sie danach Chat-ID Automatisch Abrufen."),
            ["initial.status.connectionWait"] = new("Bağlantı testi bekleniyor.", "Connection test is pending.", "Verbindungstest steht aus."),
            ["initial.button.finish"] = new("Kurulumu Tamamla", "Complete Setup", "Einrichtung Abschliessen"),
            ["initial.button.testConnection"] = new("Bağlantıyi Test Et", "Test Connection", "Verbindung Testen"),
            ["initial.button.exit"] = new("Çıkış", "Exit", "Beenden"),
            ["initial.status.testing"] = new("Test mesajı gönderiliyor, lütfen bekleyin.", "Sending test message, please wait.", "Testnachricht wird gesendet, bitte warten."),
            ["initial.telegram.testMessage"] = new("Systemcel kurulum testi ({0})", "Systemcel setup test ({0})", "Systemcel-Einrichtungstest ({0})"),
            ["initial.status.verified"] = new("Bağlantı doğrulandı. Test mesajı gönderildi.", "Connection verified. Test message sent.", "Verbindung bestaetigt. Testnachricht gesendet."),
            ["initial.message.testSuccessBody"] = new("Test mesajı başarıyla gönderildi. Kurulumu tamamlayabilirsiniz.", "Test message sent successfully. You can complete setup.", "Testnachricht wurde erfolgreich gesendet. Sie koennen die Einrichtung abschliessen."),
            ["initial.message.successTitle"] = new("Başarılı", "Success", "Erfolg"),
            ["initial.status.verifyFailed"] = new("Bağlantı doğrulanamadı. Bilgileri kontrol edip tekrar deneyin.", "Connection could not be verified. Check details and try again.", "Verbindung konnte nicht bestaetigt werden. Pruefen Sie die Angaben und versuchen Sie es erneut."),
            ["initial.error.connectionBody"] = new("Telegram test hatası: {0}", "Telegram test error: {0}", "Telegram-Testfehler: {0}"),
            ["initial.error.connectionTitle"] = new("Bağlantı Hatası", "Connection Error", "Verbindungsfehler"),
            ["initial.error.botTokenTitle"] = new("Bot Token Hatası", "Bot Token Error", "Bot-Token-Fehler"),
            ["initial.status.fetchingChatId"] = new("Chat ID için son bot mesajları okunuyor, lütfen bekleyin.", "Reading latest bot messages for Chat ID, please wait.", "Neueste Bot-Nachrichten fuer die Chat-ID werden gelesen, bitte warten."),
            ["initial.status.noMessage"] = new("Mesaj bulunamadı. Bota /start yazıp tekrar deneyin.", "No message found. Send /start to the bot and try again.", "Keine Nachricht gefunden. Senden Sie /start an den Bot und versuchen Sie es erneut."),
            ["initial.message.noMessageBody"] = new("Hiç mesaj bulunamadı.\n\nAdım:\n1) Telegram'da botunuza /start yazın.\n2) Sonra bu butona tekrar basın.", "No message was found.\n\nSteps:\n1) Send /start to your bot in Telegram.\n2) Press this button again.", "Keine Nachricht gefunden.\n\nSchritte:\n1) Senden Sie /start an Ihren Bot in Telegram.\n2) Druecken Sie diese Schaltflaeche erneut."),
            ["initial.message.chatIdNotFoundTitle"] = new("Chat ID Bulunamadı", "Chat ID Not Found", "Chat-ID Nicht Gefunden"),
            ["initial.status.chatIdSelectedFromStart"] = new("Birden fazla sohbet içinden /start mesajına gore Chat ID seçildi: {0}", "Chat ID selected from multiple chats based on /start message: {0}", "Chat-ID wurde aus mehreren Chats anhand der /start-Nachricht ausgewaehlt: {0}"),
            ["initial.message.chatIdSelectedFromStartBody"] = new("Birden fazla sohbet bulundu ({0}).\n/start mesajına gore seçilen Chat ID: {1}", "Multiple chats found ({0}).\nSelected Chat ID based on /start message: {1}", "Mehrere Chats gefunden ({0}).\nAusgewaehlte Chat-ID anhand der /start-Nachricht: {1}"),
            ["initial.message.chatIdSelectedTitle"] = new("Chat ID Seçildi", "Chat ID Selected", "Chat-ID Ausgewaehlt"),
            ["initial.status.chatIdAmbiguous"] = new("Birden fazla sohbet bulundu. Yanlış atamayı önlemek için Chat ID otomatik seçilmedi.", "Multiple chats were found. Chat ID was not auto-selected to avoid incorrect assignment.", "Mehrere Chats wurden gefunden. Die Chat-ID wurde zur Vermeidung falscher Zuordnung nicht automatisch gewaehlt."),
            ["initial.message.chatIdAmbiguousBody"] = new("Birden fazla sohbet bulundu ({0}).\n\nLütfen botunuza sadece kendi hesabınızdan /start gönderip tekrar deneyin veya Chat ID alanına değeri elle girin.", "Multiple chats found ({0}).\n\nPlease send /start to your bot only from your own account and try again, or enter Chat ID manually.", "Mehrere Chats gefunden ({0}).\n\nBitte senden Sie /start nur von Ihrem eigenen Konto an den Bot und versuchen Sie es erneut oder geben Sie die Chat-ID manuell ein."),
            ["initial.message.chatIdAmbiguousTitle"] = new("Chat ID Belirsiz", "Chat ID Ambiguous", "Chat-ID Uneindeutig"),
            ["initial.status.chatIdAuto"] = new("Chat ID otomatik alındı: {0}", "Chat ID fetched automatically: {0}", "Chat-ID wurde automatisch abgerufen: {0}"),
            ["initial.status.chatIdFetchFailed"] = new("Chat ID alınamadı. Token veya ağ bağlantısını kontrol edin.", "Chat ID could not be fetched. Check token or network connection.", "Chat-ID konnte nicht abgerufen werden. Pruefen Sie Token oder Netzwerkverbindung."),
            ["initial.error.chatIdFetchBody"] = new("Chat ID alma hatası: {0}", "Chat ID fetch error: {0}", "Fehler beim Abrufen der Chat-ID: {0}"),
            ["initial.error.telegramTitle"] = new("Telegram Hatası", "Telegram Error", "Telegram-Fehler"),
            ["initial.error.testRequiredBody"] = new("Önce \"Bağlantıyı Test Et\" adımını tamamlayın.", "Complete the \"Test Connection\" step first.", "Fuehren Sie zuerst den Schritt \"Verbindung testen\" aus."),
            ["initial.error.testRequiredTitle"] = new("Test Gerekli", "Test Required", "Test Erforderlich"),
            ["initial.error.missingInfoTitle"] = new("Eksik Bilgi", "Missing Information", "Fehlende Information"),
            ["initial.error.invalidInfoTitle"] = new("Geçersiz Bilgi", "Invalid Information", "Ungueltige Information"),
            ["initial.status.fixFields"] = new("Bağlantı testi için alanları düzeltin.", "Fix the fields to run connection test.", "Korrigieren Sie die Felder fuer den Verbindungstest."),
            ["initial.validation.botTokenRequired"] = new("Bot token zorunludur.", "Bot token is required.", "Bot-Token ist erforderlich."),
            ["initial.validation.botTokenFormat"] = new("Biçim geçersiz. Örnek: 123456789:ABCDEF123456", "Invalid format. Example: 123456789:ABCDEF123456", "Ungueltiges Format. Beispiel: 123456789:ABCDEF123456"),
            ["initial.validation.botTokenValid"] = new("Bot token biçimi uygun.", "Bot token format is valid.", "Bot-Token-Format ist gueltig."),
            ["initial.validation.userIdNumeric"] = new("User ID sayısal olmalıdır.", "User ID must be numeric.", "Benutzer-ID muss numerisch sein."),
            ["initial.validation.userIdValid"] = new("User ID geçerli görünüyor.", "User ID looks valid.", "Benutzer-ID scheint gueltig zu sein."),
            ["initial.stepHint.token"] = new("Sıradaki işlem: BotFather adımlarını tamamlayıp tokeni girin.", "Next: complete BotFather steps and enter token.", "Naechster Schritt: BotFather-Schritte abschliessen und Token eingeben."),
            ["initial.stepHint.user"] = new("Sıradaki işlem: Bota /start yazıp User ID bilgisini alın.", "Next: send /start to bot and get User ID.", "Naechster Schritt: /start an Bot senden und Benutzer-ID abrufen."),
            ["initial.stepHint.test"] = new("Sıradaki işlem: Bağlantıyi Test Et butonuna basın.", "Next: click Test Connection.", "Naechster Schritt: Auf Verbindung testen klicken."),
            ["initial.stepHint.finish"] = new("Sıradaki işlem: Kurulumu Tamamla butonuna basın.", "Next: click Complete Setup.", "Naechster Schritt: Auf Einrichtung abschliessen klicken."),
        };

        private static readonly LanguageOption[] Languages =
        {
            new() { Code = "tr", DisplayName = "Türkçe" },
            new() { Code = "en", DisplayName = "English" },
            new() { Code = "de", DisplayName = "Deutsch" }
        };

        public static string CurrentLanguage => _currentLanguage;

        public static CultureInfo CurrentCulture => _currentLanguage switch
        {
            "en" => CultureInfo.GetCultureInfo("en-US"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            _ => CultureInfo.GetCultureInfo("tr-TR")
        };

        public static IReadOnlyList<LanguageOption> SupportedLanguages => Languages;

        public static void SetLanguage(string? code)
        {
            _currentLanguage = NormalizeLanguage(code);
        }

        public static string T(string key)
        {
            if (!Texts.TryGetValue(key, out var entry))
                return key;

            return _currentLanguage switch
            {
                "en" => entry.En,
                "de" => entry.De,
                _ => entry.Tr
            };
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(CurrentCulture, T(key), args);
        }

        public static string GetTipDisplay(string canonicalTip)
        {
            return NormalizeTip(canonicalTip) == "Gider"
                ? T("tip.expense")
                : T("tip.income");
        }

        public static string NormalizeTip(string? value)
        {
            var raw = (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            return raw switch
            {
                "gelir" => "Gelir",
                "giris" => "Gelir",
                "income" => "Gelir",
                "einnahme" => "Gelir",
                "gider" => "Gider",
                "cikis" => "Gider",
                "expense" => "Gider",
                "ausgabe" => "Gider",
                _ => "Gelir"
            };
        }

        private static string NormalizeLanguage(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "en" => "en",
                "de" => "de",
                _ => "tr"
            };
        }
    }
}
