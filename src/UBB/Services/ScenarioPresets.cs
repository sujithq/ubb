using UBB.Models;

namespace UBB.Services;

public static class ScenarioPresets
{
    public static IReadOnlyList<RequestPreset> RequestPresets { get; } =
    [
        new()
        {
            Key = "normal", Label = "Normal dev request",
            Description = "Small request that stays inside the ULB and consumes the shared pool.",
            UserType = UserType.Standard, SingleRequestCredits = 1_200, PoolRemainingCredits = 390_000,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 200_000, EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "spike", Label = "Power user spike",
            Description = "Single request exceeds the universal user-level budget.",
            UserType = UserType.Standard, SingleRequestCredits = 4_000, PoolRemainingCredits = 390_000,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 200_000, EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "architect", Label = "Architect override",
            Description = "Named user has an individual budget override above the ULB.",
            UserType = UserType.Architect, SingleRequestCredits = 6_000, PoolRemainingCredits = 390_000,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 200_000, EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "poolLow", Label = "Pool exhaustion",
            Description = "The shared pool runs out mid-flow and metered mode begins.",
            UserType = UserType.Architect, SingleRequestCredits = 4_000, PoolRemainingCredits = 2_000,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 200_000, EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "ccBlock", Label = "Cost centre block",
            Description = "Pool is empty and the cost centre does not have enough metered budget.",
            UserType = UserType.Architect, SingleRequestCredits = 6_000, PoolRemainingCredits = 0,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 3_000, EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "enterpriseBlock", Label = "Enterprise hard stop",
            Description = "Pool is empty, cost centre is fine, but the enterprise cap is exhausted.",
            UserType = UserType.Architect, SingleRequestCredits = 6_000, PoolRemainingCredits = 0,
            UserUsedCredits = 0, CostCenterMeteredRemainingCredits = 200_000, EnterpriseMeteredRemainingCredits = 3_000,
        },
    ];
}
