using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class AiAssistantStatus
    {
        public bool Configured { get; set; }
        public string ProModel { get; set; } = string.Empty;
        public string FlashModel { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public AiUsageStatus Usage { get; set; } = new();
    }

    public sealed class AiUsageStatus
    {
        public bool AiAktif { get; set; }
        public string PlanKodu { get; set; } = string.Empty;
        public string PlanAdi { get; set; } = string.Empty;
        public bool SinirsizPlan { get; set; }
        public string DonemTipi { get; set; } = string.Empty;
        public int? Limit { get; set; }
        public int Kullanilan { get; set; }
        public int? Kalan { get; set; }
        public DateTime DonemBitisAt { get; set; } = DateTime.Now;
        public bool IzinVerildi { get; set; } = true;
        public bool LimitAsildi { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }

    public sealed class AiAssistantChatRequest
    {
        public string Mesaj { get; set; } = string.Empty;
        public string Mode { get; set; } = "chat";
    }

    public sealed class AiAssistantChatResponse
    {
        public bool Configured { get; set; }
        public string Mode { get; set; } = "chat";
        public string Model { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = [];
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
        public AiUsageStatus Usage { get; set; } = new();
    }

    public sealed class AiBusinessSuggestion
    {
        public string Baslik { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string Oncelik { get; set; } = "Normal";
        public string Metrik { get; set; } = string.Empty;
        public string Kaynak { get; set; } = "Systemcel";
    }

    public sealed class AiBusinessSuggestionsResponse
    {
        public bool Configured { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<AiBusinessSuggestion> Suggestions { get; set; } = [];
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
        public AiUsageStatus Usage { get; set; } = new();
    }

}
