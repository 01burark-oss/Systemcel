using CashTracker.Infrastructure.Services;
using System.Security.Cryptography;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SecretProtectorTests
    {
        [Fact]
        public void DpapiSecretProtector_EncryptsAndDecrypts_CurrentUserSecret()
        {
            var protector = new DpapiSecretProtector();
            const string secret = "gib-password-123";

            var cipher = protector.Protect(secret);

            Assert.NotEqual(secret, cipher);
            Assert.StartsWith("dpapi1:", cipher);
            Assert.True(protector.TryUnprotect(cipher, out var clear));
            Assert.Equal(secret, clear);
        }

        [Fact]
        public void DpapiSecretProtector_ReturnsFalse_ForBrokenCiphertext()
        {
            var protector = new DpapiSecretProtector();

            var ok = protector.TryUnprotect("dpapi1:not-base64", out var clear);

            Assert.False(ok);
            Assert.Equal(string.Empty, clear);
        }

        [Fact]
        public void AesGcmSecretProtector_EncryptsAuthenticatesAndDecrypts()
        {
            var protector = new AesGcmSecretProtector(RandomNumberGenerator.GetBytes(32));

            var cipher = protector.Protect("gib-password-123");

            Assert.StartsWith("aesgcm1:", cipher);
            Assert.DoesNotContain("gib-password-123", cipher);
            Assert.True(protector.TryUnprotect(cipher, out var clear));
            Assert.Equal("gib-password-123", clear);

            var payload = Convert.FromBase64String(cipher["aesgcm1:".Length..]);
            payload[^1] ^= 0x01;
            var tampered = "aesgcm1:" + Convert.ToBase64String(payload);
            Assert.False(protector.TryUnprotect(tampered, out _));
        }

        [Fact]
        public void AesGcmSecretProtector_ReadsLegacyBase64_ForOneTimeMigration()
        {
            var protector = new AesGcmSecretProtector(RandomNumberGenerator.GetBytes(32));
            var legacy = "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("legacy-secret"));

            Assert.True(protector.TryUnprotect(legacy, out var clear));
            Assert.Equal("legacy-secret", clear);
            Assert.StartsWith("aesgcm1:", protector.Protect(clear));
        }
    }
}
