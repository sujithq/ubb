namespace UBB.Models;

public class UserConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public LicenseType License { get; set; } = LicenseType.Business;
    public bool IsPowerUser { get; set; }
    public decimal? IndividualBudgetDollars { get; set; }
    public decimal DailyUsageRateCredits { get; set; } = 200;
    public Guid CostCenterId { get; set; }
}
