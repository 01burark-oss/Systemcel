using System;
using System.Collections.Generic;
using System.Linq;

namespace CashTracker.Core.Models
{
    public sealed class ClerkIdentityMigrationOptions
    {
        public ClerkIdentityMigrationOptions(IEnumerable<string>? legacyUserIds = null)
        {
            LegacyUserIds = (legacyUserIds ?? Array.Empty<string>())
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.Ordinal);
        }

        public IReadOnlySet<string> LegacyUserIds { get; }

        public bool IsEnabled => LegacyUserIds.Count > 0;
    }
}
