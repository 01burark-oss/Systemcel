using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Services;

internal sealed class LegacySecretMigrationHostedService : IHostedService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly ISecretProtector _protector;
    private readonly ILogger<LegacySecretMigrationHostedService> _logger;

    public LegacySecretMigrationHostedService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        ISecretProtector protector,
        ILogger<LegacySecretMigrationHostedService> logger)
    {
        _dbFactory = dbFactory;
        _protector = protector;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_protector is not AesGcmSecretProtector)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.GibPortalAyarlari
            .Where(x => x.SifreCipherText.StartsWith("b64:"))
            .ToListAsync(cancellationToken);
        var migrated = 0;

        foreach (var row in rows)
        {
            if (!_protector.TryUnprotect(row.SifreCipherText, out var clear))
            {
                _logger.LogWarning("GIB secret migration skipped invalid legacy ciphertext for settings row {SettingsId}.", row.Id);
                continue;
            }

            row.SifreCipherText = _protector.Protect(clear);
            row.UpdatedAt = DateTime.UtcNow;
            migrated++;
        }

        if (migrated == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Re-encrypted {Count} legacy GIB secret record(s) with AES-GCM.", migrated);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
