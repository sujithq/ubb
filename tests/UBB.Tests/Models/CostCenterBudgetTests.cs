using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Models;

/// <summary>
/// Tests for CostCenterBudget.Reset() — covers TD-07: Reset must restore the
/// configured budget, not a hardcoded constant.
/// </summary>
public class CostCenterBudgetTests
{
    // ── Reset() restores InitialMeteredBudget ─────────────────────────────────

    [Fact]
    public void Reset_RestoresMeteredBudget_ToInitialValue()
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = 150_000, InitialMeteredBudget = 150_000 };
        cc.ConsumeMetered(50_000);

        cc.Reset();

        cc.MeteredRemainingCredits.Should().Be(150_000);
    }

    [Fact]
    public void Reset_ClearsCreditsConsumed()
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = 100_000, InitialMeteredBudget = 100_000 };
        cc.ConsumeMetered(30_000);
        cc.CreditsConsumed.Should().Be(30_000);

        cc.Reset();

        cc.CreditsConsumed.Should().Be(0);
    }

    [Fact]
    public void Reset_ResetsNodeStatesToIdle()
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = 100_000, InitialMeteredBudget = 100_000 };
        cc.NodeStates[FlowNode.Pool]   = FlowNodeState.Pass;
        cc.NodeStates[FlowNode.Result] = FlowNodeState.Block;

        cc.Reset();

        cc.NodeStates.Values.Should().AllBeEquivalentTo(FlowNodeState.Idle);
    }

    [Theory]
    [InlineData(50_000)]
    [InlineData(200_000)]
    [InlineData(1)]
    [InlineData(0)]
    public void Reset_RestoresExactInitialValue_RegardlessOfAmount(int initialBudget)
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = initialBudget, InitialMeteredBudget = initialBudget };
        cc.ConsumeMetered(initialBudget);

        cc.Reset();

        cc.MeteredRemainingCredits.Should().Be(initialBudget);
    }

    // ── InitialMeteredBudget is not affected by Reset ─────────────────────────

    [Fact]
    public void Reset_DoesNotChangeInitialMeteredBudget()
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = 100_000, InitialMeteredBudget = 100_000 };
        cc.ConsumeMetered(100_000);

        cc.Reset();

        cc.InitialMeteredBudget.Should().Be(100_000);
    }

    // ── Multiple resets are idempotent ────────────────────────────────────────

    [Fact]
    public void Reset_IsIdempotent_WhenCalledMultipleTimes()
    {
        var cc = new CostCenterBudget { MeteredRemainingCredits = 75_000, InitialMeteredBudget = 75_000 };
        cc.ConsumeMetered(75_000);

        cc.Reset();
        cc.Reset();
        cc.Reset();

        cc.MeteredRemainingCredits.Should().Be(75_000);
        cc.CreditsConsumed.Should().Be(0);
    }
}
