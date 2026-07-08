using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Services;

/// <summary>
/// Tests for TD-01 enhancement: verify that logs accumulate across multiple runs,
/// and that the simulation uses current depleted state, not initial values.
/// </summary>
public class MultiCostCenterAccumulationTests
{
    // ── Logs should accumulate, not clear on each run ─────────────────────────

    [Fact]
    public void MultiCostCenter_FirstRun_PopulatesLogs()
    {
        var state = MultiCostCenterState.CreateDefault();

        RequestFlowEngine.RunMultiCostCenter(state);

        state.Logs.Should().NotBeEmpty();
        state.Logs[0].Should().Contain("Run #");
    }

    [Fact]
    public void MultiCostCenter_SecondRun_AccumulatesLogs()
    {
        var state = MultiCostCenterState.CreateDefault();
        RequestFlowEngine.RunMultiCostCenter(state);
        var firstRunLogCount = state.Logs.Count;

        RequestFlowEngine.RunMultiCostCenter(state);

        // Second run should ADD logs, not clear them
        state.Logs.Count.Should().BeGreaterThan(firstRunLogCount);
        // Should contain separator line
        state.Logs.Should().ContainMatch("*──────*");
    }

    [Fact]
    public void MultiCostCenter_LogsShouldShowDifferentPoolValues()
    {
        var state = MultiCostCenterState.CreateDefault();
        var initialPool = state.PoolRemainingCredits;

        // First run
        RequestFlowEngine.RunMultiCostCenter(state);
        var firstRunPool = state.PoolRemainingCredits;
        firstRunPool.Should().BeLessThan(initialPool, "pool should decrease during simulation");

        // Second run should start with depleted pool
        var poolBeforeSecondRun = state.PoolRemainingCredits;
        RequestFlowEngine.RunMultiCostCenter(state);

        // Logs should show the depleted pool value at start of second run, not initial
        var runHeaders = state.Logs
            .Where(l => l.Contains("[Run #"))
            .ToList();

        runHeaders.Count.Should().Be(2);
        // Each run header should show the pool value at start of that run
        // Second run header should show depleted pool (not initial)
        runHeaders[1].Should().NotContain($"| {initialPool:N0} |", "second run should show depleted pool, not initial");
    }

    // ── State mutations should persist across runs ─────────────────────────────

    [Fact]
    public void MultiCostCenter_PoolDepletesProgressively()
    {
        var state = MultiCostCenterState.CreateDefault();
        var initialPool = state.PoolRemainingCredits;

        // First run: pool decreases
        RequestFlowEngine.RunMultiCostCenter(state);
        var poolAfterRun1 = state.PoolRemainingCredits;

        // Second run: pool continues to decrease (if any requests draw from it)
        RequestFlowEngine.RunMultiCostCenter(state);
        var poolAfterRun2 = state.PoolRemainingCredits;

        poolAfterRun1.Should().BeLessThanOrEqualTo(initialPool);
        poolAfterRun2.Should().BeLessThanOrEqualTo(poolAfterRun1);
    }

    [Fact]
    public void MultiCostCenter_CostCenterBudgetsDeplete()
    {
        var state = MultiCostCenterState.CreateDefault();
        var initialEngBudget = state.CostCenters[0].MeteredRemainingCredits;

        RequestFlowEngine.RunMultiCostCenter(state);
        var engBudgetAfterRun1 = state.CostCenters[0].MeteredRemainingCredits;

        RequestFlowEngine.RunMultiCostCenter(state);
        var engBudgetAfterRun2 = state.CostCenters[0].MeteredRemainingCredits;

        // Budget should be consumed or stay same (never increase)
        engBudgetAfterRun1.Should().BeLessThanOrEqualTo(initialEngBudget);
        engBudgetAfterRun2.Should().BeLessThanOrEqualTo(engBudgetAfterRun1);
    }

    // ── Service integration: ensure AppStateService also accumulates ──────────

    [Fact]
    public void AppStateService_MultiCCRuns_AccumulateLogs()
    {
        var svc = new AppStateService();
        var state = MultiCostCenterState.CreateDefault();

        svc.RunMultiCostCenter(state);
        var firstRunLogCount = state.Logs.Count;

        svc.RunMultiCostCenter(state);

        state.Logs.Count.Should().BeGreaterThan(firstRunLogCount);
        svc.MultiCCState!.Logs.Count.Should().Be(state.Logs.Count, "service should maintain same state object");
    }
}
