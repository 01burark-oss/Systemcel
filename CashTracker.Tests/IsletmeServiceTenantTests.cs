using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class IsletmeServiceTenantTests
    {
        [Fact]
        public async Task AuthenticatedUsers_GetSeparateActiveBusinesses()
        {
            using var fixture = await IsletmeFixture.CreateAsync();
            var currentUser = new MutableCurrentUserContext();
            var service = new IsletmeService(new SingleDbContextFactory(fixture.Options), currentUser);

            currentUser.Set("user_one", "one@example.com", "User One");
            var firstUserBusiness = await service.GetActiveAsync();

            currentUser.Set("user_two", "two@example.com", "User Two");
            var secondUserBusiness = await service.GetActiveAsync();

            Assert.NotEqual(firstUserBusiness.Id, secondUserBusiness.Id);

            var secondUserRows = await service.GetAllAsync();
            Assert.Single(secondUserRows);
            Assert.Equal(secondUserBusiness.Id, secondUserRows[0].Id);

            currentUser.Set("user_one", "one@example.com", "User One");
            var firstUserRows = await service.GetAllAsync();
            Assert.Single(firstUserRows);
            Assert.Equal(firstUserBusiness.Id, firstUserRows[0].Id);
        }

        [Fact]
        public async Task FirstAuthenticatedUser_AdoptsLegacyBusinesses_SecondUserDoesNotSeeThem()
        {
            using var fixture = await IsletmeFixture.CreateAsync();
            await using (var db = fixture.CreateDbContext())
            {
                db.Isletmeler.Add(new Isletme
                {
                    Ad = "Legacy",
                    IsletmeTuru = "Genel",
                    Konum = "",
                    IsAktif = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            var currentUser = new MutableCurrentUserContext();
            var service = new IsletmeService(new SingleDbContextFactory(fixture.Options), currentUser);

            currentUser.Set("owner", "owner@example.com", "Owner");
            var adopted = await service.GetActiveAsync();

            currentUser.Set("other", "other@example.com", "Other");
            var other = await service.GetActiveAsync();

            Assert.Equal("Legacy", adopted.Ad);
            Assert.NotEqual(adopted.Id, other.Id);

            var otherRows = await service.GetAllAsync();
            Assert.Single(otherRows);
            Assert.Equal(other.Id, otherRows[0].Id);
        }

        private sealed class MutableCurrentUserContext : ICurrentUserContext
        {
            private CurrentUserIdentity? _current;

            public void Set(string providerUserId, string email, string fullName)
            {
                _current = new CurrentUserIdentity(providerUserId, email, fullName);
            }

            public CurrentUserIdentity? GetCurrentUser()
            {
                return _current;
            }
        }

        private sealed class IsletmeFixture : IDisposable
        {
            private IsletmeFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
            {
                DbPath = dbPath;
                Options = options;
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }

            public static async Task<IsletmeFixture> CreateAsync()
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_isletme_tenant_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new IsletmeFixture(dbPath, options);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext()
            {
                return new CashTrackerDbContext(Options);
            }

            public void Dispose()
            {
                try
                {
                    if (File.Exists(DbPath))
                        File.Delete(DbPath);
                }
                catch
                {
                }
            }
        }
    }
}
