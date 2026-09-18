using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class ReceiptOcrRequest
    {
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = "image/jpeg";
        public byte[] ImageBytes { get; set; } = [];
        public IReadOnlyList<string> AvailableExpenseCategories { get; set; } = [];
    }
}
