using System.IO.Compression;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Security;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Systemcel.Api.Import;
using Xunit;

namespace CashTracker.Tests;

public sealed class SecurityHardeningTests
{
    [Fact]
    public async Task ProfileImage_RejectsExtensionContentMismatch()
    {
        await using var stream = new MemoryStream(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SecureFileInspector.InspectAsync(
                stream,
                "profile.jpg",
                stream.Length,
                SecureFilePurpose.ProfileImage));

        Assert.Contains("icerik imzasi", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatAttachment_RejectsZipBombCompressionRatio()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("payload.txt", CompressionLevel.SmallestSize);
            await using var output = entry.Open();
            await output.WriteAsync(new byte[2 * 1024 * 1024]);
        }
        stream.Position = 0;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SecureFileInspector.InspectAsync(
                stream,
                "payload.zip",
                stream.Length,
                SecureFilePurpose.ChatAttachment));

        Assert.Contains("sikistirma", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatAttachment_UsesSignatureInsteadOfClaimedMimeType()
    {
        await using var stream = new MemoryStream("%PDF-1.7\n%%EOF"u8.ToArray());

        var result = await SecureFileInspector.InspectAsync(
            stream,
            "document.pdf",
            stream.Length,
            SecureFilePurpose.ChatAttachment);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(".pdf", result.Extension);
    }

    [Fact]
    public async Task ConversationAndAttachment_BlockUnrelatedTenant()
    {
        var root = Path.Combine(Path.GetTempPath(), $"systemcel_security_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "tenant.db");
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var factory = new SingleDbContextFactory(options);
        int conversationId;
        int attachmentId;
        var attachmentPath = Path.Combine(root, "chat-attachments", "1", "safe.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(attachmentPath)!);
        await File.WriteAllTextAsync(attachmentPath, "%PDF-1.7\n%%EOF");

        await using (var db = new CashTrackerDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.AddRange(
                Business(1, "Accountant", "Muhasebeci"),
                Business(2, "Customer", "Isletme"),
                Business(3, "Outsider", "Isletme"));
            var conversation = new MuhasebeciSohbet
            {
                MuhasebeciIsletmeId = 1,
                MusteriIsletmeId = 2,
                Konu = "Tenant security"
            };
            db.MuhasebeciSohbetleri.Add(conversation);
            await db.SaveChangesAsync();
            var attachment = new MuhasebeciSohbetEki
            {
                SohbetId = conversation.Id,
                YukleyenIsletmeId = 2,
                DosyaAdi = "safe.pdf",
                IcerikTipi = "application/pdf",
                DosyaYolu = attachmentPath,
                Boyut = new FileInfo(attachmentPath).Length
            };
            db.MuhasebeciSohbetEkleri.Add(attachment);
            await db.SaveChangesAsync();
            conversationId = conversation.Id;
            attachmentId = attachment.Id;
        }

        var businessService = new FakeIsletmeService { Active = Business(3, "Outsider", "Isletme") };
        var service = new MuhasebeciSohbetMerkeziService(
            factory,
            businessService,
            new MuhasebeciSohbetStorageOptions { AppDataPath = root });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetMesajlarAsync(conversationId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DosyaIndirAsync(attachmentId));

        businessService.Active = Business(2, "Customer", "Isletme");
        var allowed = await service.DosyaIndirAsync(attachmentId);
        Assert.Equal(Path.GetFullPath(attachmentPath), allowed.DosyaYolu);

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task AttachmentDownload_RejectsDatabasePathOutsideStorageRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"systemcel_security_root_{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"systemcel_outside_{Guid.NewGuid():N}.pdf");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(outside, "%PDF-1.7\n%%EOF");
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseSqlite($"Data Source={Path.Combine(root, "tenant.db")}")
            .Options;
        var factory = new SingleDbContextFactory(options);
        int attachmentId;
        await using (var db = new CashTrackerDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.AddRange(Business(1, "Accountant", "Muhasebeci"), Business(2, "Customer", "Isletme"));
            var conversation = new MuhasebeciSohbet { MuhasebeciIsletmeId = 1, MusteriIsletmeId = 2, Konu = "Path" };
            db.MuhasebeciSohbetleri.Add(conversation);
            await db.SaveChangesAsync();
            var attachment = new MuhasebeciSohbetEki
            {
                SohbetId = conversation.Id,
                YukleyenIsletmeId = 2,
                DosyaAdi = "outside.pdf",
                IcerikTipi = "application/pdf",
                DosyaYolu = outside,
                Boyut = new FileInfo(outside).Length
            };
            db.MuhasebeciSohbetEkleri.Add(attachment);
            await db.SaveChangesAsync();
            attachmentId = attachment.Id;
        }

        var service = new MuhasebeciSohbetMerkeziService(
            factory,
            new FakeIsletmeService { Active = Business(2, "Customer", "Isletme") },
            new MuhasebeciSohbetStorageOptions { AppDataPath = root });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DosyaIndirAsync(attachmentId));

        try { Directory.Delete(root, true); } catch { }
        try { File.Delete(outside); } catch { }
    }

    [Fact]
    public async Task DesktopImportCode_IsOwnerBoundAndAtomicallySingleUse()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_import_code_{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var factory = new SingleDbContextFactory(options);
        await using (var db = new CashTrackerDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.Add(Business(1, "Owner", "Isletme"));
            await db.SaveChangesAsync();
        }

        var store = new DesktopImportCodeStore(factory);
        var created = await store.CreateAsync(1, "user-owner");
        Assert.Null(await store.FindAsync(created.Code, "other-user"));

        var claimed = await store.ClaimAsync(created.Code, "user-owner");
        await Assert.ThrowsAsync<DesktopImportValidationException>(() =>
            store.ClaimAsync(created.Code, "user-owner"));

        await store.MarkUsedAsync(claimed, "package-1", new CashTracker.Core.Import.DesktopImportTotals());
        await Assert.ThrowsAsync<DesktopImportValidationException>(() =>
            store.ClaimAsync(created.Code, "user-owner"));

        await using (var db = new CashTrackerDbContext(options))
        {
            var row = await db.DesktopImportKodlari.SingleAsync();
            Assert.Equal(DesktopImportCodeStatus.Used, row.Status);
            Assert.Equal("user-owner", row.RequestedBy);
            Assert.Equal(1, row.TargetIsletmeId);
        }

        try { File.Delete(dbPath); } catch { }
    }

    private static Isletme Business(int id, string name, string tenantType) => new()
    {
        Id = id,
        Ad = name,
        TenantTipi = tenantType,
        IsletmeTuru = "Genel",
        Konum = string.Empty,
        IsAktif = true
    };
}
