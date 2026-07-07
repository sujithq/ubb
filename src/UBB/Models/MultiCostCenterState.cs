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
    /// Log of requests processed in this simulation.
    /// </summary>
    public List<string> Logs { get; set; } = [];

    /// <summary>
    /// Create a default multi-CC state with 3 cost centers.
    /// </summary>
    public static MultiCostCenterState CreateDefault() => new()
    {
        CostCenters =
        [
            new() { Name = "Engineering", UserCount = 10, MeteredRemainingCredits = 200_000 },
            new() { Name = "Research", UserCount = 5, MeteredRemainingCredits = 150_000 },
            new() { Name = "Sales", UserCount = 3, MeteredRemainingCredits = 100_000 },
        ],
        PoolRemainingCredits = 390_000,
        EnterpriseMeteredRemainingCredits = 1_000_000,
    };

    /// <summary>
    /// Reset all cost centers and logs.
    /// </summary>
    public void Reset()
    {
        foreach (var cc in CostCenters)
        {
            cc.Reset(200_000); // Default metered budget
        }
        PoolRemainingCredits = 390_000;
        EnterpriseMeteredRemainingCredits = 1_000_000;
        Logs.Clear();
    }

    /// <summary>
    /// Add a log entry.
    /// </summary>
    public void AddLog(string message) => Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
}
