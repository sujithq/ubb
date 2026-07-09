namespace UBB.Models;

public class SimulationConfig
{
    public int BusinessSeats { get; set; } = 50;
    public int EnterpriseSeats { get; set; } = 20;
    /// <summary>Business 3,000 / Enterprise 7,000 credits/seat (promo period June–Sept 2026).</summary>
    public bool UsePromotionalCredits { get; set; } = false;
    /// <summary>Mirrors "AI credit paid usage" policy. If false, usage stops when pool exhausted.</summary>
    public bool AllowMeteredUsage { get; set; } = true;
    /// <summary>Universal user-level budget in dollars. Most specific ULB wins. Always hard stop.</summary>
    public decimal? UniversalUserLevelBudgetDollars { get; set; }
    /// <summary>Caps metered charges only. Total bill = license fees + metered charges.</summary>
    public decimal? EnterpriseMeteredBudgetDollars { get; set; }
    /// <summary>"Stop usage when budget limit is reached" for enterprise budget. Default false.</summary>
    public bool StopOnEnterpriseMeteredExhaustion { get; set; } = false;
}
