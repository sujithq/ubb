using UBB.Models;

namespace UBB.Services;

public static class BillingConstants
{
    public const decimal CreditValueDollars = 0.01m;
    public const int BusinessStandardCredits = 1_900;
    public const int BusinessPromoCredits = 3_000;
    public const int EnterpriseStandardCredits = 3_900;
    public const int EnterprisePromoCredits = 7_000;
    public const decimal BusinessSeatCost = 19m;
    public const decimal EnterpriseSeatCost = 39m;

    public static int CreditsPerSeat(LicenseType license, bool promo) => (license, promo) switch
    {
        (LicenseType.Business,   false) => BusinessStandardCredits,
        (LicenseType.Business,   true)  => BusinessPromoCredits,
        (LicenseType.Enterprise, false) => EnterpriseStandardCredits,
        (LicenseType.Enterprise, true)  => EnterprisePromoCredits,
        _ => 0
    };

    /// <summary>Convert a dollar budget to its equivalent credit limit.</summary>
    public static decimal DollarsToCredits(decimal dollars) => dollars / CreditValueDollars;

    /// <summary>Convert a credit amount to dollars.</summary>
    public static decimal CreditsToDollars(decimal credits) => credits * CreditValueDollars;
}
