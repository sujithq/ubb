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
        new()
        {
            Key = "multiUnequalBudgets", Label = "Multi-CC Unequal Budgets",
            Description = "CCs have different metered budgets; first CC consumes pool, second gets metered, third blocked due to low CC budget.",
            CostCenters =
            [
                ("Engineering", 10, 250_000), // Large metered budget
                ("Research", 5, 150_000),     // Medium metered budget
                ("Support", 2, 8_000),        // Small metered budget (problem child)
            ],
            PoolRemainingCredits = 2_000,
            EnterpriseMeteredRemainingCredits = 500_000,
        },
        new()
        {
            Key = "multiCCBottleneck", Label = "One CC Bottleneck",
            Description = "One cost center has exhausted its metered budget (2k request blocks), while others have pool/metered available.",
            CostCenters =
            [
                ("Engineering", 10, 300_000),
                ("Research", 5, 50),         // Exhausted metered (only 50 credits left)
                ("Sales", 3, 150_000),
            ],
            PoolRemainingCredits = 100_000,
            EnterpriseMeteredRemainingCredits = 800_000,
        },
        new()
        {
            Key = "multiTightEnterprise", Label = "Tight Enterprise Cap",
            Description = "Pool has capacity, CCs have metered, but enterprise cap is very restrictive (only 2k left for 3 requests).",
            CostCenters =
            [
                ("Engineering", 10, 200_000),
                ("Research", 5, 200_000),
                ("Sales", 3, 200_000),
            ],
            PoolRemainingCredits = 50_000,
            EnterpriseMeteredRemainingCredits = 2_000, // Enterprise cap blocks 3rd CC
        },
        new()
        {
            Key = "multiSequentialMetered", Label = "Sequential Metered Consumption",
            Description = "Pool exhausts after 1st CC, all 3 CCs end up in metered with enterprise cap as final arbiter.",
            CostCenters =
            [
                ("Engineering", 10, 8_000),  // Enough for one 2k request
                ("Research", 5, 8_000),     // Enough for one 2k request
                ("Sales", 3, 8_000),        // Enough for one 2k request but enterprise blocks
            ],
            PoolRemainingCredits = 1_500, // Not enough for first CC
            EnterpriseMeteredRemainingCredits = 10_000, // Enough for ~5k (blocks 3rd CC)
        },
        new()
        {
            Key = "multiLargeSpike", Label = "Large Spike Exhausts Pool",
            Description = "First CC spike request (3k) exhausts pool immediately; remaining CCs compete in metered phase.",
            CostCenters =
            [
                ("Engineering", 10, 300_000), // First: makes 3k request
                ("Research", 5, 100_000),    // Second: must use metered
                ("Sales", 3, 100_000),       // Third: must use metered if enterprise cap allows
            ],
            PoolRemainingCredits = 2_500, // Allows partial spike, then exhausted
            EnterpriseMeteredRemainingCredits = 6_000, // Enough for ~3k more (all 3 CCs at 2k each = 6k)
        },
        new()
        {
            Key = "multiEdgeCase", Label = "Multi-CC Edge Case",
            Description = "Pool exactly equals first CC request; second CC enters metered; third CC blocked at enterprise cap.",
            CostCenters =
            [
                ("Engineering", 10, 300_000),
                ("Research", 5, 300_000),
                ("Sales", 3, 300_000),
            ],
            PoolRemainingCredits = 2_000, // Exactly one CC request
            EnterpriseMeteredRemainingCredits = 2_100, // Enough for ~1 request in metered
        },
    ];
}
