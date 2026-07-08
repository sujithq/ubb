using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Engine;

/// <summary>
/// Tests for BillingConstants — conversions and seat credit calculations.
/// </summary>
public class BillingConstantsTests
{
    [Fact]
    public void CreditValue_Is_OneCent() =>
        BillingConstants.CreditValueDollars.Should().Be(0.01m);

    [Theory]
    [InlineData(100,  1.00)]
    [InlineData(1_000, 10.00)]
    [InlineData(390_000, 3_900.00)]
    public void CreditsToDollars_ConvertsCorrectly(decimal credits, decimal expectedDollars) =>
        BillingConstants.CreditsToDollars(credits).Should().Be(expectedDollars);

    [Theory]
    [InlineData(1.00,  100)]
    [InlineData(10.00, 1_000)]
    [InlineData(19.00, 1_900)]
    public void DollarsToCredits_ConvertsCorrectly(decimal dollars, decimal expectedCredits) =>
        BillingConstants.DollarsToCredits(dollars).Should().Be(expectedCredits);

    [Theory]
    [InlineData(LicenseType.Business,   false, 1_900)]
    [InlineData(LicenseType.Business,   true,  3_000)]
    [InlineData(LicenseType.Enterprise, false, 3_900)]
    [InlineData(LicenseType.Enterprise, true,  7_000)]
    public void CreditsPerSeat_ReturnsCorrectValue(LicenseType license, bool promo, int expected) =>
        BillingConstants.CreditsPerSeat(license, promo).Should().Be(expected);

    [Fact]
    public void BusinessSeatCost_Is_19() =>
        BillingConstants.BusinessSeatCost.Should().Be(19m);

    [Fact]
    public void EnterpriseSeatCost_Is_39() =>
        BillingConstants.EnterpriseSeatCost.Should().Be(39m);
}
