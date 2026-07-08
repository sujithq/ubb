using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Engine;

/// <summary>
/// Unit tests for RequestFlowEngine.EvaluateStep — the 5-checkpoint single-request billing flow.
/// Billing flow order:
///   1. ULB check  → always hard stop
///   2. Pool check → draw from shared pool
///   3. Metered    → cost centre budget → enterprise budget
/// </summary>
public class RequestFlowEngineTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static FlowResult Evaluate(
        decimal credits        = 1_000,
        decimal userUsed       = 0,
        decimal userLimit      = 2_500,
        decimal poolRemaining  = 390_000,
        decimal ccRemaining    = 200_000,
        decimal entRemaining   = 1_000_000)
        => RequestFlowEngine.EvaluateStep("test", credits, userUsed, userLimit,
                                          poolRemaining, ccRemaining, entRemaining);

    private static FlowNodeState NodeOf(FlowResult r, string key)
        => r.NodeStates.TryGetValue(key, out var s) ? s : FlowNodeState.Idle;

    // ── Step 1: ULB check ────────────────────────────────────────────────────

    [Fact]
    public void ULB_WhenRequestFitsWithinLimit_Passes()
    {
        var result = Evaluate(credits: 1_000, userUsed: 0, userLimit: 2_500);

        result.Blocked.Should().BeFalse();
        NodeOf(result, "user").Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void ULB_WhenRequestExceedsLimit_Blocks()
    {
        var result = Evaluate(credits: 4_000, userUsed: 0, userLimit: 2_500);

        result.Blocked.Should().BeTrue();
        NodeOf(result, "user").Should().Be(FlowNodeState.Block);
        NodeOf(result, "result").Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void ULB_WhenCumulativeUsageExceedsLimit_Blocks()
    {
        // user already used 2000, new request of 1000 would push to 3000 > 2500 limit
        var result = Evaluate(credits: 1_000, userUsed: 2_000, userLimit: 2_500);

        result.Blocked.Should().BeTrue();
        NodeOf(result, "user").Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void ULB_WhenRequestExactlyEqualsLimit_Passes()
    {
        var result = Evaluate(credits: 2_500, userUsed: 0, userLimit: 2_500);

        result.Blocked.Should().BeFalse();
        NodeOf(result, "user").Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void ULB_WhenArchitectOverrideUsed_AllowsLargerRequest()
    {
        // Architect individual limit = 8000
        var result = Evaluate(credits: 6_000, userUsed: 0, userLimit: 8_000);

        result.Blocked.Should().BeFalse();
        NodeOf(result, "user").Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void ULB_Block_DoesNotMutatePoolOrBudgets()
    {
        var result = Evaluate(credits: 5_000, userUsed: 0, userLimit: 2_500,
                               poolRemaining: 390_000, ccRemaining: 200_000, entRemaining: 1_000_000);

        result.PoolRemainingCredits.Should().Be(390_000);
        result.CostCenterMeteredRemainingCredits.Should().Be(200_000);
        result.EnterpriseMeteredRemainingCredits.Should().Be(1_000_000);
        result.UserUsedCredits.Should().Be(0); // not incremented on block
    }

    // ── Step 2: Pool check ───────────────────────────────────────────────────

    [Fact]
    public void Pool_WhenPoolSufficient_DrawsFromPool()
    {
        var result = Evaluate(credits: 1_000, poolRemaining: 390_000);

        result.Blocked.Should().BeFalse();
        result.PoolRemainingCredits.Should().Be(389_000);
        NodeOf(result, "pool").Should().Be(FlowNodeState.Pass);
        NodeOf(result, "result").Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void Pool_WhenPoolExactlyCoversRequest_DrawsEntirePool()
    {
        var result = Evaluate(credits: 1_000, poolRemaining: 1_000);

        result.Blocked.Should().BeFalse();
        result.PoolRemainingCredits.Should().Be(0);
        NodeOf(result, "pool").Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void Pool_WhenPoolCoversRequest_DoesNotTouchMeteredBudgets()
    {
        var result = Evaluate(credits: 1_000, poolRemaining: 390_000,
                               ccRemaining: 200_000, entRemaining: 1_000_000);

        result.CostCenterMeteredRemainingCredits.Should().Be(200_000);
        result.EnterpriseMeteredRemainingCredits.Should().Be(1_000_000);
    }

    // ── Step 3a: Metered — partial pool ─────────────────────────────────────

    [Fact]
    public void Metered_WhenPoolPartiallyCoversRequest_WarnsThenChargesMetered()
    {
        // Pool has 500, request is 2000 → 500 from pool, 1500 from metered
        var result = Evaluate(credits: 2_000, poolRemaining: 500,
                               ccRemaining: 200_000, entRemaining: 1_000_000);

        result.Blocked.Should().BeFalse();
        result.PoolRemainingCredits.Should().Be(0);
        result.CostCenterMeteredRemainingCredits.Should().Be(198_500); // 200000 - 1500
        result.EnterpriseMeteredRemainingCredits.Should().Be(998_500); // 1000000 - 1500
        NodeOf(result, "pool").Should().Be(FlowNodeState.Warn);
        NodeOf(result, "paid").Should().Be(FlowNodeState.Warn);
        NodeOf(result, "result").Should().Be(FlowNodeState.Warn);
    }

    [Fact]
    public void Metered_WhenPoolEmpty_AllCreditsChargedToMetered()
    {
        var result = Evaluate(credits: 2_000, poolRemaining: 0,
                               ccRemaining: 200_000, entRemaining: 1_000_000);

        result.Blocked.Should().BeFalse();
        result.CostCenterMeteredRemainingCredits.Should().Be(198_000);
        result.EnterpriseMeteredRemainingCredits.Should().Be(998_000);
        NodeOf(result, "pool").Should().Be(FlowNodeState.Block);
    }

    // ── Step 3b: Cost centre block ───────────────────────────────────────────

    [Fact]
    public void CostCentre_WhenMeteredBudgetInsufficient_Blocks()
    {
        // Pool empty, CC budget only 3000, request is 6000.
        // userLimit must be >= 6000 to reach the CC check (otherwise ULB fires first).
        var result = Evaluate(credits: 6_000, userUsed: 0, userLimit: 10_000,
                               poolRemaining: 0, ccRemaining: 3_000, entRemaining: 1_000_000);

        result.Blocked.Should().BeTrue();
        NodeOf(result, "costCentre").Should().Be(FlowNodeState.Block);
        NodeOf(result, "result").Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void CostCentre_WhenMeteredBudgetExactlyCoversMeteredPart_Passes()
    {
        // Pool has 1000, request is 3000, so 2000 metered. CC has exactly 2000.
        // userLimit must be >= 3000 to avoid ULB block before reaching this check.
        var result = Evaluate(credits: 3_000, userUsed: 0, userLimit: 10_000,
                               poolRemaining: 1_000, ccRemaining: 2_000, entRemaining: 1_000_000);

        result.Blocked.Should().BeFalse();
        NodeOf(result, "costCentre").Should().Be(FlowNodeState.Pass);
        result.CostCenterMeteredRemainingCredits.Should().Be(0);
    }

    [Fact]
    public void CostCentre_Block_DoesNotMutateEnterpriseBudget()
    {
        var result = Evaluate(credits: 6_000, poolRemaining: 0,
                               ccRemaining: 3_000, entRemaining: 1_000_000);

        result.EnterpriseMeteredRemainingCredits.Should().Be(1_000_000);
    }

    // ── Step 3c: Enterprise cap block ────────────────────────────────────────

    [Fact]
    public void Enterprise_WhenCapInsufficient_Blocks()
    {
        // Pool empty, CC has budget, enterprise cap only 3000, request is 6000.
        // userLimit must be >= 6000 to reach the enterprise check.
        var result = Evaluate(credits: 6_000, userUsed: 0, userLimit: 10_000,
                               poolRemaining: 0, ccRemaining: 200_000, entRemaining: 3_000);

        result.Blocked.Should().BeTrue();
        NodeOf(result, "enterprise").Should().Be(FlowNodeState.Block);
        NodeOf(result, "result").Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void Enterprise_WhenCapExactlyCoversMeteredPart_Passes()
    {
        var result = Evaluate(credits: 2_000, poolRemaining: 0,
                               ccRemaining: 200_000, entRemaining: 2_000);

        result.Blocked.Should().BeFalse();
        NodeOf(result, "enterprise").Should().Be(FlowNodeState.Pass);
        result.EnterpriseMeteredRemainingCredits.Should().Be(0);
    }

    [Fact]
    public void Enterprise_Block_DoesNotMutateCostCentreBudget()
    {
        var result = Evaluate(credits: 6_000, poolRemaining: 0,
                               ccRemaining: 200_000, entRemaining: 3_000);

        result.CostCenterMeteredRemainingCredits.Should().Be(200_000);
    }

    // ── State mutation assertions ─────────────────────────────────────────────

    [Fact]
    public void UserUsedCredits_IncreasedByRequestAmount_OnPass()
    {
        var result = Evaluate(credits: 1_500, userUsed: 0, poolRemaining: 390_000);

        result.UserUsedCredits.Should().Be(1_500);
    }

    [Fact]
    public void UserUsedCredits_IncreasedByRequestAmount_OnMeteredPass()
    {
        var result = Evaluate(credits: 2_000, userUsed: 0, poolRemaining: 0,
                               ccRemaining: 200_000, entRemaining: 1_000_000);

        result.UserUsedCredits.Should().Be(2_000);
    }

    [Fact]
    public void UserUsedCredits_NotIncremented_OnBlock()
    {
        var result = Evaluate(credits: 5_000, userUsed: 500, userLimit: 2_500);

        result.UserUsedCredits.Should().Be(500); // unchanged
    }

    // ── Log assertions ────────────────────────────────────────────────────────

    [Fact]
    public void Logs_ContainBlockKeyword_WhenBlocked()
    {
        var result = Evaluate(credits: 5_000, userLimit: 2_500);

        result.Logs.Should().Contain(l => l.Contains("BLOCK"));
    }

    [Fact]
    public void Logs_ContainPassKeyword_WhenPoolCoversRequest()
    {
        var result = Evaluate(credits: 1_000, poolRemaining: 390_000);

        result.Logs.Should().Contain(l => l.Contains("PASS"));
    }

    [Fact]
    public void Logs_ContainWarnKeyword_WhenMeteredPhaseActive()
    {
        var result = Evaluate(credits: 2_000, poolRemaining: 0,
                               ccRemaining: 200_000, entRemaining: 1_000_000);

        result.Logs.Should().Contain(l => l.Contains("WARN"));
    }
}
