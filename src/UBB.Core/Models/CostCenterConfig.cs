namespace UBB.Models;

public class CostCenterConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<UserConfig> Users { get; set; } = [];
    /// <summary>Per-member cap active in both pool and metered phases. Always a hard stop.</summary>
    public decimal? CostCenterUserLevelBudgetDollars { get; set; }
    /// <summary>Caps metered charges only (after pool exhausted).</summary>
    public decimal? MeteredBudgetDollars { get; set; }
    /// <summary>Whether to block when metered budget is exhausted. Default false (GitHub default).</summary>
    public bool StopOnMeteredExhaustion { get; set; } = false;
    /// <summary>Simulator-only: GitHub auto-sets this from seat count. Manual here for what-if modelling.</summary>
    public decimal? IncludedCreditAllocationCap { get; set; }
    public bool BlockOnIncludedCap { get; set; } = false;
    /// <summary>If true, this CC's metered charges don't count against the enterprise budget.</summary>
    public bool ExcludedFromEnterpriseBudget { get; set; } = false;
}
