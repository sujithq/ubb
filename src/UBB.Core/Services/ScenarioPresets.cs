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
            Description = "3 cost centers with multiple users each; shared pool covers all requests.",
            CostCenters =
            [
                ("Engineering", 10, 500_000),   // 10 users × 2k = 20k per run
                ("Research", 5, 300_000),       // 5 users × 2k = 10k per run
                ("Sales", 3, 200_000),          // 3 users × 2k = 6k per run
            ],
            PoolRemainingCredits = 200_000,      // Enough for ~3-4 runs
            EnterpriseMeteredRemainingCredits = 2_000_000,
        },
        new()
        {
            Key = "multiPoolExhaustion", Label = "Multi-CC Pool Exhaustion",
            Description = "Pool exhaustion mid-flow as users request credits; metered phase begins for later CCs.",
            CostCenters =
            [
                ("Engineering", 10, 500_000),   // 10 users × 2k = 20k
                ("Research", 5, 300_000),       // 5 users × 2k = 10k
                ("Sales", 3, 200_000),          // 3 users × 2k = 6k
            ],
            PoolRemainingCredits = 25_000,       // Engineering gets most from pool, Research hits metered
            EnterpriseMeteredRemainingCredits = 2_000_000,
        },
        new()
        {
            Key = "multiEnterpriseBlock", Label = "Multi-CC Enterprise Block",
            Description = "Pool exhausted, CCs have metered budgets, but enterprise cap limits total consumption. Sales CC blocked.",
            CostCenters =
            [
                ("Engineering", 10, 500_000),
                ("Research", 5, 300_000),
                ("Sales", 3, 200_000),
            ],
            PoolRemainingCredits = 0,
            EnterpriseMeteredRemainingCredits = 30_000,  // 10+5 users fit (30k), 3 more users (6k) are blocked
        },
        new()
        {
            Key = "multiUnequalBudgets", Label = "Multi-CC Unequal Budgets",
            Description = "CCs have different metered budgets; Support CC with limited budget gets blocked first.",
            CostCenters =
            [
                ("Engineering", 10, 600_000),   // Large metered budget
                ("Research", 5, 300_000),       // Medium metered budget
                ("Support", 2, 3_000),          // Small metered budget (2 users × 2k = 4k > 3k, so blocks on 2nd user)
            ],
            PoolRemainingCredits = 10_000,
            EnterpriseMeteredRemainingCredits = 1_000_000,
        },
        new()
        {
            Key = "multiCCBottleneck", Label = "One CC Bottleneck",
            Description = "Research CC has exhausted its metered budget; blocking its users while others proceed.",
            CostCenters =
            [
                ("Engineering", 10, 600_000),
                ("Research", 5, 2_000),         // Exhausted (2 users × 2k = 4k > 2k available)
                ("Sales", 3, 400_000),
            ],
            PoolRemainingCredits = 200_000,
            EnterpriseMeteredRemainingCredits = 1_500_000,
        },
        new()
        {
            Key = "multiTightEnterprise", Label = "Tight Enterprise Cap",
            Description = "Pool and CC budgets available, but enterprise cap severely limits total metered consumption.",
            CostCenters =
            [
                ("Engineering", 10, 500_000),
                ("Research", 5, 500_000),
                ("Sales", 3, 500_000),
            ],
            PoolRemainingCredits = 100_000,
            EnterpriseMeteredRemainingCredits = 25_000,  // Only enough for ~1-2 CCs in metered phase
        },
        new()
        {
            Key = "multiSequentialMetered", Label = "Sequential Metered Consumption",
            Description = "Pool exhausts early; all CCs shift to metered phase; enterprise cap determines winners.",
            CostCenters =
            [
                ("Engineering", 10, 100_000),   // 10 users × 2k = 20k, limited metered
                ("Research", 5, 100_000),       // 5 users × 2k = 10k
                ("Sales", 3, 100_000),          // 3 users × 2k = 6k
            ],
            PoolRemainingCredits = 5_000,        // Barely enough for 1st user
            EnterpriseMeteredRemainingCredits = 50_000,  // All CCs must use it; decisions to be made
        },
        new()
        {
            Key = "multiLargeSpike", Label = "Large Spike Exhausts Pool",
            Description = "Engineering request surge exhausts pool; remaining CCs compete in metered phase.",
            CostCenters =
            [
                ("Engineering", 10, 600_000),   // 10 users × 2k = 20k (the spike)
                ("Research", 5, 200_000),       // 5 users × 2k = 10k
                ("Sales", 3, 150_000),          // 3 users × 2k = 6k
            ],
            PoolRemainingCredits = 22_000,       // Almost covers Engineering, metered for others
            EnterpriseMeteredRemainingCredits = 100_000,  // Enough for Research + partial Sales
        },
        new()
        {
            Key = "multiEdgeCase", Label = "Multi-CC Edge Case",
            Description = "Pool barely covers first CC; remaining CCs compete in metered; enterprise decides final outcome.",
            CostCenters =
            [
                ("Engineering", 10, 500_000),   // 10 users × 2k = 20k
                ("Research", 5, 500_000),       // 5 users × 2k = 10k
                ("Sales", 3, 500_000),          // 3 users × 2k = 6k
            ],
            PoolRemainingCredits = 21_000,       // Exactly covers Engineering + 1k buffer
            EnterpriseMeteredRemainingCredits = 20_000,  // Enough for Research, blocks Sales
        },
    ];
}
