using CashTracker.Core.Entities;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class MuhasebeciApiTests
{
    [Theory]
    [InlineData("Muhasebeci", true, true)]
    [InlineData("Isletme", false, true)]
    [InlineData("Isletme", true, false)]
    public void ProfilResmiYukleme_YalnizMuhasebeciVeyaTamamlanmamisKurulumdaAciktir(
        string tenantTipi,
        bool kolayKurulumTamamlandi,
        bool expected)
    {
        var activeBusiness = new Isletme
        {
            TenantTipi = tenantTipi,
            KolayKurulumTamamlandi = kolayKurulumTamamlandi
        };

        Assert.Equal(expected, MuhasebeciApi.CanUploadProfileImage(activeBusiness));
    }
}
