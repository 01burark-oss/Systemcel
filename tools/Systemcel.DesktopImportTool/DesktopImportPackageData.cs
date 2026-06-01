using CashTracker.Core.Import;

namespace Systemcel.DesktopImportTool;

internal sealed class DesktopImportPackageData
{
    public List<DesktopImportIsletmeRecord> Isletmeler { get; } = new();
    public List<DesktopImportCariKartRecord> CariKartlar { get; } = new();
    public List<DesktopImportCariHareketRecord> CariHareketler { get; } = new();
    public List<DesktopImportUrunHizmetRecord> Urunler { get; } = new();
    public List<DesktopImportStokHareketRecord> StokHareketleri { get; } = new();
    public List<DesktopImportFaturaRecord> Faturalar { get; } = new();
    public List<DesktopImportFaturaSatirRecord> FaturaSatirlari { get; } = new();
    public List<DesktopImportTahsilatOdemeRecord> TahsilatOdemeler { get; } = new();
    public List<DesktopImportKasaHareketRecord> KasaHareketleri { get; } = new();

    public DesktopImportTotals BuildTotals()
    {
        return new DesktopImportTotals
        {
            Isletme = Isletmeler.Count,
            CariKart = CariKartlar.Count,
            CariHareket = CariHareketler.Count,
            UrunHizmet = Urunler.Count,
            StokHareket = StokHareketleri.Count,
            Fatura = Faturalar.Count,
            FaturaSatir = FaturaSatirlari.Count,
            TahsilatOdeme = TahsilatOdemeler.Count,
            KasaHareket = KasaHareketleri.Count
        };
    }
}
