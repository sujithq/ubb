namespace UBB.Models;

/// <summary>
/// Represents a cost center with its own metered budget and a list of users.
/// </summary>
public class CostCenterBudget
{
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int MeteredRemainingCredits { get; set; }
    public int CreditsConsumed { get; set; }

    /// <summary>
    /// The budget value at creation time, used by Reset() to restore the original amount.
    /// </summary>
    public int InitialMeteredBudget { get; set; }

    /// <summary>
    /// Per-CC node states set by the engine after simulation. Keys: pool, paid, costCentre, enterprise, result.
    /// </summary>
    public Dictionary<string, FlowNodeState> NodeStates { get; set; } = DefaultNodeStates();

    public static Dictionary<string, FlowNodeState> DefaultNodeStates() => new()
    {
        ["pool"]       = FlowNodeState.Idle,
        ["paid"]       = FlowNodeState.Idle,
        ["costCentre"] = FlowNodeState.Idle,
        ["enterprise"] = FlowNodeState.Idle,
        ["result"]     = FlowNodeState.Idle,
    };

    public void ResetNodeStates() => NodeStates = DefaultNodeStates();

    /// <summary>
    /// Returns true if this cost center has metered budget remaining.
    /// </summary>
    public bool HasMeteredBudget => MeteredRemainingCredits > 0;

    /// <summary>
    /// Consume credits from this cost center's metered budget.
    /// Returns the amount actually consumed (may be less if budget exhausted).
    /// </summary>
    public int ConsumeMetered(int requestCredits)
    {
        if (requestCredits <= 0) return 0;
        
        int consumed = Math.Min(requestCredits, MeteredRemainingCredits);
        MeteredRemainingCredits -= consumed;
        CreditsConsumed += consumed;
        return consumed;
    }

    /// <summary>
    /// Reset to the budget value captured at creation time (InitialMeteredBudget).
    /// </summary>
    public void Reset()
    {
        MeteredRemainingCredits = InitialMeteredBudget;
        CreditsConsumed = 0;
        ResetNodeStates();
    }
}
