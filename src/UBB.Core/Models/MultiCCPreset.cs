namespace UBB.Models;

/// <summary>
/// Configuration for a multi-cost-center billing scenario.
/// </summary>
public class MultiCCPreset
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of cost centers in this preset (name, user count, metered budget).
    /// </summary>
    public List<(string Name, int Users, int MeteredBudget)> CostCenters { get; set; } = [];

    public int PoolRemainingCredits { get; set; }
    public int EnterpriseMeteredRemainingCredits { get; set; }

    /// <summary>
    /// Apply this preset to a MultiCostCenterState.
    /// </summary>
    public void ApplyTo(MultiCostCenterState state)
    {
        state.CostCenters.Clear();
        foreach (var (name, users, budget) in CostCenters)
        {
            state.CostCenters.Add(new CostCenterBudget
            {
                Name                  = name,
                UserCount             = users,
                MeteredRemainingCredits = budget,
                InitialMeteredBudget  = budget,  // store for Reset()
                CreditsConsumed       = 0
            });
        }
        state.PoolRemainingCredits                     = PoolRemainingCredits;
        state.InitialPoolRemainingCredits              = PoolRemainingCredits;
        state.EnterpriseMeteredRemainingCredits        = EnterpriseMeteredRemainingCredits;
        state.InitialEnterpriseMeteredRemainingCredits = EnterpriseMeteredRemainingCredits;
        state.Logs.Clear();
    }
}
