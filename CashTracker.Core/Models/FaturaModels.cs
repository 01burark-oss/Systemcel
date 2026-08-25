using System;
using System.Collections.Generic;
using CashTracker.Core.Entities;

namespace CashTracker.Core.Models
{
    public static class FaturaDurum
    {
        public const string YerelTaslak = "YerelTaslak";
        public const string PortalTaslak = "PortalTaslak";
        public const string Kesildi = "Kesildi";
        public const string KismiOdendi = "KismiOdendi";
        public const string Odendi = "Odendi";
        public const string Iptal = "Iptal";
    }

    public sealed class FaturaSatirRequest
    {
        public int? UrunHizmetId { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string Birim { get; set; } = "Adet";
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal IskontoOrani { get; set; }
        public decimal KdvOrani { get; set; } = 20m;
        public bool StokEtkilesin { get; set; } = true;
    }

    public sealed class FaturaCreateRequest
    {
        public int CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public DateTime? VadeTarihi { get; set; }
        public string FaturaTipi { get; set; } = "Satis";
        public string OdemeYontemi { get; set; } = "Nakit";
        public string ParaBirimi { get; set; } = "TRY";
        public string? Aciklama { get; set; }
        public List<FaturaSatirRequest> Satirlar { get; set; } = [];
    }

    public sealed class FaturaTotals
    {
        public decimal AraToplam { get; set; }
        public decimal IskontoToplam { get; set; }
        public decimal KdvToplam { get; set; }
        public decimal GenelToplam { get; set; }
    }

    public sealed class FaturaDetail
    {
        public Fatura Fatura { get; set; } = new();
        public CariKart? Cari { get; set; }
        public List<FaturaSatir> Satirlar { get; set; } = [];
    }

    public sealed class TahsilatOdemeRequest
    {
        public int FaturaId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public decimal Tutar { get; set; }
        public string OdemeYontemi { get; set; } = "Nakit";
        public string? ParaBirimi { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class TahsilatOdemeHareketGuncelleRequest
    {
        public string IslemTipi { get; set; } = "Tahsilat";
        public int CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public decimal Tutar { get; set; }
        public string OdemeYontemi { get; set; } = "Nakit";
        public string? Aciklama { get; set; }
    }

    public sealed class GibPortalSettingsModel
    {
        public string KullaniciKodu { get; set; } = string.Empty;
        public bool HasPassword { get; set; }
        public bool TestModu { get; set; }
    }

    public sealed class GibPortalSaveSettingsRequest
    {
        public string KullaniciKodu { get; set; } = string.Empty;
        public string? Sifre { get; set; }
        public bool TestModu { get; set; }
    }

    public sealed class GibPortalLogModel
    {
        public int Id { get; init; }
        public int? FaturaId { get; init; }
        public DateTime Tarih { get; init; }
        public string Islem { get; init; } = string.Empty;
        public bool Basarili { get; init; }
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class GibPortalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public string BelgeNo { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;

        public static GibPortalResult Ok(string message, string uuid = "", string belgeNo = "", string operationId = "")
        {
            return new GibPortalResult { Success = true, Message = message, Uuid = uuid, BelgeNo = belgeNo, OperationId = operationId };
        }

        public static GibPortalResult Fail(string message)
        {
            return new GibPortalResult { Success = false, Message = message };
        }
    }
}
