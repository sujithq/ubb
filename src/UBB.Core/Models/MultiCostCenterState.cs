namespace UBB.Models;

/// <summary>
/// Represents a multi-cost-center billing simulation state.
/// Shows how multiple cost centers compete for shared pool and enterprise cap.
/// </summary>
public class MultiCostCenterState
{
    /// <summary>
    /// List of cost centers in this simulation.
    /// </summary>
    public List<CostCenterBudget> CostCenters { get; set; } = [];

    /// <summary>
    /// Shared pool remaining (all CCs draw from this first).
    /// </summary>
    public int PoolRemainingCredits { get; set; }

    /// <summary>
    /// Enterprise metered cap (all CCs draw from this during metered phase).
    /// </summary>
    public int EnterpriseMeteredRemainingCredits { get; set; }

    /// <summary>
    /// Request size per cost center in credits (configurable).
    /// </summary>
    public int RequestCreditsPerCC { get; set; } = 2000;

    /// <summary>
    /// Log of requests processed in this simulation.
    /// </summary>
    public List<string> Logs { get; set; } = [];

    /// <summary>
    /// Create a default multi-CC state with 3 cost centers.
    /// </summary>
    /// <summary>Shared pool value at creation time — restored by Reset().</summary>
    public int InitialPoolRemainingCredits { get; set; }

    /// <summary>Enterprise cap value at creation time — restored by Reset().</summary>
    public int InitialEnterpriseMeteredRemainingCredits { get; set; }

    public static MultiCostCenterState CreateDefault() => new()
    {
        CostCenters =
        [
            new() { Name = "Engineering", UserCount = 10, MeteredRemainingCredits = 200_000, InitialMeteredBudget = 200_000 },
            new() { Name = "Research",    UserCount = 5,  MeteredRemainingCredits = 150_000, InitialMeteredBudget = 150_000 },
            new() { Name = "Sales",       UserCount = 3,  MeteredRemainingCredits = 100_000, InitialMeteredBudget = 100_000 },
        ],
        PoolRemainingCredits                      = 390_000,
        InitialPoolRemainingCredits               = 390_000,
        EnterpriseMeteredRemainingCredits         = 1_000_000,
        InitialEnterpriseMeteredRemainingCredits  = 1_000_000,
    };

    /// <summary>
    /// Reset all cost centers and org-level budgets to their initial values.
    /// </summary>
    public void Reset()
    {
        foreach (var cc in CostCenters)
            cc.Reset(); // restores each CC's InitialMeteredBudget
        PoolRemainingCredits              = InitialPoolRemainingCredits;
        EnterpriseMeteredRemainingCredits = InitialEnterpriseMeteredRemainingCredits;
        Logs.Clear();
    }

    /// <summary>
    /// Add a log entry.
    /// </summary>
    public void AddLog(string message) => Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
}
