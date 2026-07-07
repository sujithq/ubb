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
    /// Reset for next day or scenario.
    /// </summary>
    public void Reset(int initialMeteredBudget)
    {
        MeteredRemainingCredits = initialMeteredBudget;
        CreditsConsumed = 0;
    }
}
