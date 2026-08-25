using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models;

public sealed class SubeOlusturRequest
{
    public string Ad { get; init; } = string.Empty;
    public string Kod { get; init; } = string.Empty;
}

public sealed class DovizKuruKaydetRequest
{
    public string ParaBirimi { get; init; } = "TRY";
    public decimal Kur { get; init; }
    public DateTime? GecerliAt { get; init; }
}

public sealed class SubeDto
{
    public int Id { get; init; }
    public string Ad { get; init; } = string.Empty;
    public string Kod { get; init; } = string.Empty;
    public bool Varsayilan { get; init; }
    public bool Aktif { get; init; }
}

public sealed class DovizKuruDto
{
    public string ParaBirimi { get; init; } = "TRY";
    public decimal Kur { get; init; }
    public DateTime GecerliAt { get; init; }
}

public sealed class SubeKurDurumuDto
{
    public SubeDto AktifSube { get; init; } = new();
    public List<SubeDto> Subeler { get; init; } = [];
    public List<DovizKuruDto> Kurlar { get; init; } = [];
    public bool CokluSubeAktif { get; init; }
    public bool CokluParaBirimiAktif { get; init; }
}

public sealed class SubeOlusturResult
{
    public SubeDto Sube { get; init; } = new();
    public bool Tekrarlandi { get; init; }
}

public sealed class KurKaydetResult
{
    public DovizKuruDto Kur { get; init; } = new();
    public bool Tekrarlandi { get; init; }
}

public sealed class IslemKurSnapshot
{
    public int SubeId { get; init; }
    public string ParaBirimi { get; init; } = "TRY";
    public decimal Kur { get; init; } = 1m;
    public decimal OrijinalTutar { get; init; }
    public decimal TryKarsiligi { get; init; }
}

public sealed class SubeFinansOzetiDto
{
    public int? SubeId { get; init; }
    public bool Konsolide { get; init; }
    public decimal GelirTry { get; init; }
    public decimal GiderTry { get; init; }
    public decimal NetTry { get; init; }
    public List<ParaBirimiOzetiDto> ParaBirimleri { get; init; } = [];
}

public sealed class ParaBirimiOzetiDto
{
    public string ParaBirimi { get; init; } = "TRY";
    public decimal GelirOrijinal { get; init; }
    public decimal GiderOrijinal { get; init; }
    public decimal GelirTry { get; init; }
    public decimal GiderTry { get; init; }
}
