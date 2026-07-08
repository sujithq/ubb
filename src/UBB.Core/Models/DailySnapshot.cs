namespace UBB.Models;

public class DailySnapshot
{
    public int Day { get; set; }
    public decimal RemainingPoolCredits { get; set; }
    public decimal CumulativeMeteredSpendDollars { get; set; }
    public Dictionary<Guid, CostCenterDailySnapshot> CostCenterSnapshots { get; set; } = [];
    public List<Guid> BlockedUsers { get; set; } = [];
    public List<Guid> BlockedCostCenters { get; set; } = [];
}

public class CostCenterDailySnapshot
{
    public decimal CreditsDrawnFromPool { get; set; }
    public decimal CumulativeMeteredSpend { get; set; }
    public bool IsBlockedByMeteredBudget { get; set; }
    public bool IsBlockedByIncludedCap { get; set; }
}
