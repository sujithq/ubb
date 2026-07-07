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

    /// <summary>
    /// Multi-cost-center scenario presets showing organizational-scale billing dynamics.
    /// </summary>
    public static IReadOnlyList<MultiCCPreset> MultiCCPresets { get; } =
    [
        new()
        {
            Key = "multiNormal", Label = "Multi-CC Normal",
            Description = "3 cost centers make reasonable requests; shared pool covers all.",
            CostCenters =
            [
                ("Engineering", 10, 200_000),
                ("Research", 5, 150_000),
                ("Sales", 3, 100_000),
            ],
            PoolRemainingCredits = 390_000,
            EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "multiPoolExhaustion", Label = "Multi-CC Pool Exhaustion",
            Description = "3 CCs make larger requests (2k each); pool exhausted mid-flow, metered mode begins.",
            CostCenters =
            [
                ("Engineering", 10, 200_000),
                ("Research", 5, 150_000),
                ("Sales", 3, 100_000),
            ],
            PoolRemainingCredits = 4_000, // Only enough for 2 CCs, 3rd hits metered
            EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "multiEnterpriseBlock", Label = "Multi-CC Enterprise Block",
            Description = "Pool exhausted, CCs have metered budgets, but enterprise cap is low (only 4k left).",
            CostCenters =
            [
                ("Engineering", 10, 200_000),
                ("Research", 5, 150_000),
                ("Sales", 3, 100_000),
            ],
            PoolRemainingCredits = 0,
            EnterpriseMeteredRemainingCredits = 4_000, // Only enough for 2 CC requests
        },
    ];
}
