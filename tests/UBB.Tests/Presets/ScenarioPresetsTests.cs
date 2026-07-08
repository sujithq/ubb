using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Presets;

/// <summary>
/// Tests for ScenarioPresets — verifies preset data integrity and that all presets
/// produce expected outcomes when run through the engine.
/// </summary>
public class ScenarioPresetsTests
{
    // ── Data integrity ────────────────────────────────────────────────────────

    [Fact]
    public void AllRequestPresets_HaveUniqueKeys()
    {
        var keys = ScenarioPresets.RequestPresets.Select(p => p.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllMultiCCPresets_HaveUniqueKeys()
    {
        var keys = ScenarioPresets.MultiCCPresets.Select(p => p.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllRequestPresets_HaveNonEmptyLabelsAndDescriptions()
    {
        ScenarioPresets.RequestPresets.Should().AllSatisfy(p =>
        {
            p.Label.Should().NotBeNullOrWhiteSpace();
            p.Description.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void AllMultiCCPresets_HaveAtLeastOneCostCenter()
    {
        ScenarioPresets.MultiCCPresets.Should().AllSatisfy(p =>
            p.CostCenters.Should().NotBeEmpty());
    }

    [Fact]
    public void AllMultiCCPresets_HaveNonNegativePoolAndEnterpriseCap()
    {
        ScenarioPresets.MultiCCPresets.Should().AllSatisfy(p =>
        {
            p.PoolRemainingCredits.Should().BeGreaterThanOrEqualTo(0);
            p.EnterpriseMeteredRemainingCredits.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    // ── Preset outcome smoke tests ────────────────────────────────────────────
    // ULB defaults: Standard = 2500, Architect individual = 8000 (from RequestFlowState defaults)

    private static FlowResult RunPreset(RequestPreset p) =>
        RequestFlowEngine.EvaluateStep("test",
            p.SingleRequestCredits, p.UserUsedCredits,
            p.UserType == UserType.Architect ? 8_000m : 2_500m,
            p.PoolRemainingCredits, p.CostCenterMeteredRemainingCredits,
            p.EnterpriseMeteredRemainingCredits);

    [Fact]
    public void Preset_Normal_ProducesPassResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "normal"));

        result.Blocked.Should().BeFalse();
        result.NodeStates[FlowNode.Result].Should().Be(FlowNodeState.Pass);
    }

    [Fact]
    public void Preset_Spike_ProducesBlockResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "spike"));

        result.Blocked.Should().BeTrue();
    }

    [Fact]
    public void Preset_Architect_ProducesPassResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "architect"));

        result.Blocked.Should().BeFalse();
    }

    [Fact]
    public void Preset_PoolLow_ProducesMeteredWarnResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "poolLow"));

        result.Blocked.Should().BeFalse();
        result.NodeStates[FlowNode.Result].Should().Be(FlowNodeState.Warn);
    }

    [Fact]
    public void Preset_CCBlock_ProducesBlockResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "ccBlock"));

        result.Blocked.Should().BeTrue();
        result.NodeStates[FlowNode.CostCentre].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void Preset_EnterpriseBlock_ProducesBlockResult()
    {
        var result = RunPreset(ScenarioPresets.RequestPresets.Single(p => p.Key == "enterpriseBlock"));

        result.Blocked.Should().BeTrue();
        result.NodeStates[FlowNode.Enterprise].Should().Be(FlowNodeState.Block);
    }

    // ── Multi-CC preset smoke tests ───────────────────────────────────────────

    [Fact]
    public void MultiPreset_Normal_AllCCsPass()
    {
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiNormal");
        var state  = MultiCostCenterState.CreateDefault();
        preset.ApplyTo(state);

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters.Should().AllSatisfy(cc =>
            cc.NodeStates[FlowNode.Result].Should().Be(FlowNodeState.Pass));
    }

    [Fact]
    public void MultiPreset_EnterpriseBlock_LastCCBlocked()
    {
        var preset = ScenarioPresets.MultiCCPresets.Single(p => p.Key == "multiEnterpriseBlock");
        var state  = MultiCostCenterState.CreateDefault();
        preset.ApplyTo(state);

        RequestFlowEngine.RunMultiCostCenter(state);

        state.CostCenters.Should().Contain(cc =>
            cc.NodeStates[FlowNode.Result] == FlowNodeState.Block);
    }
}
