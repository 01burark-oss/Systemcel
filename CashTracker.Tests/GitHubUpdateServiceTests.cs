using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class GitHubUpdateServiceTests
    {
        [Fact]
        public async Task CheckAsync_WhenSetupAssetExists_ReturnsInstallerUpdate()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/repos/owner/repo/releases/latest", StringComparison.Ordinal))
                {
                    return RecordingHttpMessageHandler.OkJson("""
                    {
                      "tag_name": "v2.0.0",
                      "html_url": "https://example.test/release",
                      "body": "Stable release",
                      "assets": [
                        {
                          "name": "CashTracker-Setup.exe",
                          "browser_download_url": "https://example.test/CashTracker-Setup.exe"
                        },
                        {
                          "name": "CashTracker-Setup.exe.sha256",
                          "browser_download_url": "https://example.test/CashTracker-Setup.exe.sha256"
                        },
                        {
                          "name": "CashTracker-v2.0.0.zip",
                          "browser_download_url": "https://example.test/CashTracker-v2.0.0.zip"
                        }
                      ]
                    }
                    """);
                }

                throw new HttpRequestException($"Unexpected request: {url}");
            });

            using var http = new HttpClient(handler);
            var service = new GitHubUpdateService(http);

            var result = await service.CheckAsync(
                new UpdateSettings
                {
                    RepoOwner = "owner",
                    RepoName = "repo"
                },
                "1.9.0");

            Assert.True(result.IsConfigured);
            Assert.True(result.HasUpdate);
            Assert.Equal("v2.0.0", result.LatestTag);
            Assert.Equal("CashTracker-Setup.exe", result.AssetName);
            Assert.True(result.CanInstallInApp);
            Assert.Equal("CashTracker-Setup.exe.sha256", result.ChecksumAssetName);
            Assert.Equal("https://example.test/release", result.ReleasePageUrl);
        }

        [Fact]
        public async Task ResolveExpectedSha256Async_DownloadsAndParsesChecksum()
        {
            const string expectedSha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{expectedSha256} *CashTracker-Setup.exe")
                    };
                }

                throw new HttpRequestException($"Unexpected request: {url}");
            });

            using var http = new HttpClient(handler);
            var service = new GitHubUpdateService(http);

            var result = new UpdateCheckResult(
                true,
                true,
                "v2.0.0",
                "CashTracker-Setup.exe",
                "https://example.test/CashTracker-Setup.exe",
                "CashTracker-Setup.exe.sha256",
                "https://example.test/CashTracker-Setup.exe.sha256",
                "https://example.test/release",
                "Stable release");

            var actual = await service.ResolveExpectedSha256Async(result);

            Assert.Equal(expectedSha256, actual);
        }

        [Fact]
        public void VerifyDownloadedAssetHash_ValidatesExpectedHash()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "cashtracker");
                using var stream = File.OpenRead(tempFile);
                var expectedSha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();

                GitHubUpdateService.VerifyDownloadedAssetHash(tempFile, expectedSha256);

                var error = Assert.Throws<InvalidOperationException>(() =>
                    GitHubUpdateService.VerifyDownloadedAssetHash(tempFile, new string('0', 64)));
                Assert.Equal("Indirilen paket dogrulanamadi.", error.Message);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
