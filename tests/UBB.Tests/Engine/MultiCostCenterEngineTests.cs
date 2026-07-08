using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Engine;

/// <summary>
/// Unit tests for RequestFlowEngine.RunMultiCostCenter — sequential CC request processing.
/// Each CC makes one request of configurable size; pool is shared; metered budgets per CC + enterprise cap.
/// </summary>
public class MultiCostCenterEngineTests
{
    private static MultiCostCenterState BuildState(
        int requestCreditsPerCC     = 2_000,
        int poolRemaining           = 390_000,
        int enterpriseRemaining     = 1_000_000,
        params (string Name, int Users, int MeteredBudget)[] costCenters)
    {
        var state = new MultiCostCenterState
        {
            RequestCreditsPerCC               = requestCreditsPerCC,
            PoolRemainingCredits              = poolRemaining,
            EnterpriseMeteredRemainingCredits = enterpriseRemaining,
        };
        foreach (var (name, users, budget) in costCenters)
            state.CostCenters.Add(new CostCenterBudget { Name = name, UserCount = users, MeteredRemainingCredits = budget });
        return state;
    }

    // ── All-pass scenarios ────────────────────────────────────────────────────

    [Fact]
    public void AllCCs_Pass_WhenPoolCoversAll()
    {
        // 1 user per CC to keep numbers simple in unit test
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 6_100, enterpriseRemaining: 1_000_000,
            ("Eng", 1, 200_000), ("Research", 1, 150_000), ("Sales", 1, 100_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters.Should().AllSatisfy(cc => cc.NodeStates["result"].Should().Be(FlowNodeState.Pass));
        state.PoolRemainingCredits.Should().Be(100); // 6100 - 3×2k
        state.EnterpriseMeteredRemainingCredits.Should().Be(1_000_000); // untouched
    }

    [Fact]
    public void AllCCs_Pass_WhenMeteredBudgetsSufficient_AndPoolEmpty()
    {
        // 1 user per CC: 2 users × 2k = 4k metered consumption
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 1_000_000,
            ("Eng", 1, 200_000), ("Research", 1, 150_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters.Should().AllSatisfy(cc =>
            cc.NodeStates["result"].Should().Be(FlowNodeState.Warn)); // metered pass
        state.EnterpriseMeteredRemainingCredits.Should().Be(996_000); // 1M - 2×2k
    }

    // ── Pool exhaustion ───────────────────────────────────────────────────────

    [Fact]
    public void Pool_PartiallyCoversFirstCC_ThenExhausts()
    {
        // Pool has 1500 — not enough for 1 user's 2000 request
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 1_500, enterpriseRemaining: 1_000_000,
            ("Eng", 1, 200_000), ("Research", 1, 150_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.PoolRemainingCredits.Should().Be(0);
        state.CostCenters[0].NodeStates["pool"].Should().Be(FlowNodeState.Warn);
        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Warn);
        // Second CC has no pool left, goes metered
        state.CostCenters[1].NodeStates["pool"].Should().Be(FlowNodeState.Block);
        state.CostCenters[1].NodeStates["result"].Should().Be(FlowNodeState.Warn);
    }

    [Fact]
    public void FirstCC_DrawsFromPool_SecondCC_GoesMetered()
    {
        // Pool exactly covers first CC only (1 user × 2k = 2k)
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 4_000, enterpriseRemaining: 1_000_000,
            ("Eng", 1, 200_000), ("Research", 1, 150_000), ("Sales", 1, 100_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Pass);  // Gets 2k from pool
        state.CostCenters[1].NodeStates["result"].Should().Be(FlowNodeState.Pass);  // Gets remaining 2k from pool
        state.CostCenters[2].NodeStates["result"].Should().Be(FlowNodeState.Warn);  // No pool left, uses 2k metered
        state.PoolRemainingCredits.Should().Be(0);
    }

    // ── CC budget block ───────────────────────────────────────────────────────

    [Fact]
    public void CC_Blocked_WhenItsMeteredBudgetInsufficient()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 1_000_000,
            ("Eng", 10, 200_000),
            ("Broke", 5, 500));  // only 500 credits, needs 2000

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Warn); // metered pass
        state.CostCenters[1].NodeStates["result"].Should().Be(FlowNodeState.Block);
        state.CostCenters[1].NodeStates["costCentre"].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void CC_Block_DoesNotConsumeEnterpriseBudget()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 1_000_000,
            ("Broke", 5, 500));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.EnterpriseMeteredRemainingCredits.Should().Be(1_000_000);
    }

    [Fact]
    public void BlockedCC_DoesNotConsumeItsOwnBudget()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 1_000_000,
            ("Broke", 5, 500));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters[0].MeteredRemainingCredits.Should().Be(500); // unchanged
        state.CostCenters[0].CreditsConsumed.Should().Be(0);
    }

    // ── Enterprise cap block ─────────────────────────────────────────────────

    [Fact]
    public void ThirdCC_Blocked_WhenEnterpriseLimitReached()
    {
        // Enterprise only covers 2 × 2000 = 4000; third CC blocked
        // 1 user per CC: total 3 users × 2k = 6k needed
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 4_000,
            ("Eng", 1, 200_000), ("Research", 1, 150_000), ("Sales", 1, 100_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Warn);
        state.CostCenters[1].NodeStates["result"].Should().Be(FlowNodeState.Warn);
        state.CostCenters[2].NodeStates["result"].Should().Be(FlowNodeState.Block);
        state.CostCenters[2].NodeStates["enterprise"].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void Enterprise_Block_DoesNotMutateCC_MeteredBudget()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 2_000,
            ("Eng", 10, 200_000), ("Research", 5, 200_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        // First passes, second is blocked by enterprise cap
        state.CostCenters[1].MeteredRemainingCredits.Should().Be(200_000); // unchanged
    }

    // ── Node state correctness ────────────────────────────────────────────────

    [Fact]
    public void NodeStates_ResetToIdle_BeforeEachSimulation()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 390_000, enterpriseRemaining: 1_000_000,
            ("Eng", 10, 200_000));

        // Run once (pass)
        RequestFlowEngine.RunMultiCostCenter(state);
        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Pass);

        // Drain pool and run again (should now be metered)
        state.PoolRemainingCredits = 0;
        RequestFlowEngine.RunMultiCostCenter(state);
        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Warn);
    }

    [Fact]
    public void PoolPass_SetsCorrectNodeStates()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 390_000, enterpriseRemaining: 1_000_000,
            ("Eng", 10, 200_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        var cc = state.CostCenters[0];
        cc.NodeStates["pool"].Should().Be(FlowNodeState.Pass);
        cc.NodeStates["paid"].Should().Be(FlowNodeState.Idle);
        cc.NodeStates["costCentre"].Should().Be(FlowNodeState.Idle);
        cc.NodeStates["enterprise"].Should().Be(FlowNodeState.Idle);
        cc.NodeStates["result"].Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void MeteredPass_SetsCorrectNodeStates()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 0, enterpriseRemaining: 1_000_000,
            ("Eng", 10, 200_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        var cc = state.CostCenters[0];
        cc.NodeStates["pool"].Should().Be(FlowNodeState.Block);
        cc.NodeStates["paid"].Should().Be(FlowNodeState.Warn);
        cc.NodeStates["costCentre"].Should().Be(FlowNodeState.Pass);
        cc.NodeStates["enterprise"].Should().Be(FlowNodeState.Pass);
        cc.NodeStates["result"].Should().Be(FlowNodeState.Warn);
    }

    // ── Configurable request size ─────────────────────────────────────────────

    [Fact]
    public void LargeRequest_ExhaustsPoolFaster()
    {
        // 1 user making 10k request (larger than default 2k)
        var state = BuildState(requestCreditsPerCC: 10_000, poolRemaining: 15_000, enterpriseRemaining: 1_000_000,
            ("Eng", 1, 200_000), ("Research", 1, 200_000));

        RequestFlowEngine.RunMultiCostCenter(state);

        // First CC draws 10k from pool (5k left), second CC draws 5k from pool + 5k metered
        state.PoolRemainingCredits.Should().Be(0);
        state.CostCenters[0].NodeStates["result"].Should().Be(FlowNodeState.Pass);
        state.CostCenters[1].NodeStates["result"].Should().Be(FlowNodeState.Warn);
    }

    // ── Logs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Logs_AccumulatedAcrossRuns()
    {
        var state = BuildState(requestCreditsPerCC: 2_000, poolRemaining: 390_000, enterpriseRemaining: 1_000_000,
            ("Eng", 10, 200_000));

        RequestFlowEngine.RunMultiCostCenter(state);
        var countAfterFirst = state.Logs.Count;

        RequestFlowEngine.RunMultiCostCenter(state);
        // Logs should accumulate, not reset to same count
        state.Logs.Count.Should().BeGreaterThan(countAfterFirst);
    }
}
