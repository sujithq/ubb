using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Models;

/// <summary>
/// Tests for MultiCostCenterState.Reset() and preset application — covers TD-07:
/// Reset must restore initial pool, enterprise cap, and per-CC budgets, not hardcoded values.
/// </summary>
public class MultiCostCenterStateTests
{
    // ── CreateDefault() snapshot ──────────────────────────────────────────────

    [Fact]
    public void CreateDefault_SetsInitialValuesMatchingCurrent()
    {
        var state = MultiCostCenterState.CreateDefault();

        state.InitialPoolRemainingCredits.Should().Be(state.PoolRemainingCredits);
        state.InitialEnterpriseMeteredRemainingCredits.Should().Be(state.EnterpriseMeteredRemainingCredits);
        state.CostCenters.Should().AllSatisfy(cc =>
            cc.InitialMeteredBudget.Should().Be(cc.MeteredRemainingCredits));
    }

    // ── Reset() restores org-level budgets ────────────────────────────────────

    [Fact]
    public void Reset_RestoresPoolToInitialValue()
    {
        var state = MultiCostCenterState.CreateDefault();
        var initialPool = state.PoolRemainingCredits;
        state.PoolRemainingCredits = 0; // simulate exhaustion

        state.Reset();

        state.PoolRemainingCredits.Should().Be(initialPool);
    }

    [Fact]
    public void Reset_RestoresEnterpriseCapToInitialValue()
    {
        var state = MultiCostCenterState.CreateDefault();
        var initialEnt = state.EnterpriseMeteredRemainingCredits;
        state.EnterpriseMeteredRemainingCredits = 0;

        state.Reset();

        state.EnterpriseMeteredRemainingCredits.Should().Be(initialEnt);
    }

    [Fact]
    public void Reset_ClearsLogs()
    {
        var state = MultiCostCenterState.CreateDefault();
        RequestFlowEngine.RunMultiCostCenter(state);
        state.Logs.Should().NotBeEmpty();

        state.Reset();

        state.Logs.Should().BeEmpty();
    }

    // ── Reset() restores per-CC budgets ───────────────────────────────────────

    [Fact]
    public void Reset_RestoresEachCC_ToItsOwnInitialBudget()
    {
        var state = MultiCostCenterState.CreateDefault();
        // Capture individual initial budgets (they differ per CC)
        var initialBudgets = state.CostCenters.Select(cc => cc.InitialMeteredBudget).ToList();

        // Exhaust pool so CCs consume metered budget
        state.PoolRemainingCredits = 0;
        RequestFlowEngine.RunMultiCostCenter(state);

        state.Reset();

        state.CostCenters.Select(cc => cc.MeteredRemainingCredits)
             .Should().BeEquivalentTo(initialBudgets);
    }

    [Fact]
    public void Reset_RestoresDistinctBudgets_WhenCCsHaveDifferentValues()
    {
        // Engineering=200k, Research=150k, Sales=100k — must restore correctly, not all to the same value
        var state = MultiCostCenterState.CreateDefault();
        state.PoolRemainingCredits = 0;
        RequestFlowEngine.RunMultiCostCenter(state);

        state.Reset();

        state.CostCenters[0].MeteredRemainingCredits.Should().Be(200_000); // Engineering
        state.CostCenters[1].MeteredRemainingCredits.Should().Be(150_000); // Research
        state.CostCenters[2].MeteredRemainingCredits.Should().Be(100_000); // Sales
    }

    // ── Full simulate → reset → simulate cycle ────────────────────────────────

    [Fact]
    public void SimulateResetSimulate_ProducesSameOutcome_BothTimes()
    {
        var state = MultiCostCenterState.CreateDefault();
        // Use pool-exhaustion preset so metered paths are exercised
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiPoolExhaustion");
        preset.ApplyTo(state);

        RequestFlowEngine.RunMultiCostCenter(state);
        var logsFirstRun = state.Logs.ToList();
        var poolAfterFirst = state.PoolRemainingCredits;

        state.Reset();
        // After reset, state should be back to preset values
        state.PoolRemainingCredits.Should().Be(preset.PoolRemainingCredits);

        RequestFlowEngine.RunMultiCostCenter(state);
        var logsSecondRun = state.Logs.ToList();

        logsSecondRun.Should().BeEquivalentTo(logsFirstRun);
        state.PoolRemainingCredits.Should().Be(poolAfterFirst);
    }

    // ── MultiCCPreset.ApplyTo captures initial values ─────────────────────────

    [Fact]
    public void ApplyTo_SetsInitialPoolAndEnterpriseFromPreset()
    {
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiEnterpriseBlock");
        var state  = MultiCostCenterState.CreateDefault();
        preset.ApplyTo(state);

        state.InitialPoolRemainingCredits.Should().Be(preset.PoolRemainingCredits);
        state.InitialEnterpriseMeteredRemainingCredits.Should().Be(preset.EnterpriseMeteredRemainingCredits);
    }

    // ── TD-14: AddLog uses injectable clock for deterministic timestamps ──────

    [Fact]
    public void AddLog_UsesInjectedClock_ForDeterministicTimestamps()
    {
        var state = MultiCostCenterState.CreateDefault();
        state.Clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 9, 14, 30, 45, TimeSpan.Zero));

        state.AddLog("hello");

        state.Logs.Should().ContainSingle().Which.Should().Be("[14:30:45] hello");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public void ApplyTo_SetsInitialMeteredBudget_ForEachCC()
    {
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiNormal");
        var state  = MultiCostCenterState.CreateDefault();
        preset.ApplyTo(state);

        foreach (var (cc, (_, _, budget)) in state.CostCenters.Zip(preset.CostCenters))
            cc.InitialMeteredBudget.Should().Be(budget);
    }

    [Fact]
    public void ApplyTo_ThenReset_RestoresPresetValues()
    {
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiNormal");
        var state  = MultiCostCenterState.CreateDefault();
        preset.ApplyTo(state);

        // Simulate drains pool
        RequestFlowEngine.RunMultiCostCenter(state);
        state.Reset();

        state.PoolRemainingCredits.Should().Be(preset.PoolRemainingCredits);
        state.EnterpriseMeteredRemainingCredits.Should().Be(preset.EnterpriseMeteredRemainingCredits);
    }
}
