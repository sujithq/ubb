using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Services;

/// <summary>
/// Plain xUnit tests for AppStateService — no rendering needed;
/// the service has no Blazor-specific dependencies beyond the event.
///
/// Covers TD-10: SetUserType and SetMode must clear ActivePresetKey so the
/// preset badge does not show stale state after the user changes mode/type.
/// </summary>
public class AppStateServiceTests
{
    private static AppStateService ServiceWithPreset(string key = "normal")
    {
        var svc = new AppStateService();
        var preset = ScenarioPresets.RequestPresets.Single(p => p.Key == key);
        svc.ApplyPreset(preset);
        return svc;
    }

    // ── ApplyPreset sets the key ──────────────────────────────────────────────

    [Fact]
    public void ApplyPreset_SetsActivePresetKey()
    {
        var svc = new AppStateService();
        var preset = ScenarioPresets.RequestPresets.Single(p => p.Key == "normal");

        svc.ApplyPreset(preset);

        svc.ActivePresetKey.Should().Be("normal");
    }

    // ── TD-10: SetUserType clears ActivePresetKey ─────────────────────────────

    [Fact]
    public void SetUserType_ClearsActivePresetKey()
    {
        var svc = ServiceWithPreset("architect");

        svc.SetUserType(UserType.Standard);

        svc.ActivePresetKey.Should().BeNull();
    }

    [Fact]
    public void SetUserType_ClearsKey_EvenWhenSameTypeAsPreset()
    {
        var svc = ServiceWithPreset("normal"); // normal = Standard
        svc.FlowState.UserType.Should().Be(UserType.Standard);

        svc.SetUserType(UserType.Standard); // set same type

        svc.ActivePresetKey.Should().BeNull();
    }

    // ── TD-10: SetMode clears ActivePresetKey ─────────────────────────────────

    [Fact]
    public void SetMode_ClearsActivePresetKey()
    {
        var svc = ServiceWithPreset("normal");

        svc.SetMode(SimulationMode.Agentic);

        svc.ActivePresetKey.Should().BeNull();
    }

    [Fact]
    public void SetMode_ClearsKey_ForAllModes()
    {
        foreach (var mode in Enum.GetValues<SimulationMode>())
        {
            var svc = ServiceWithPreset("normal");
            svc.SetMode(mode);
            svc.ActivePresetKey.Should().BeNull($"because SetMode({mode}) should clear the preset key");
        }
    }

    // ── Reset clears ActivePresetKey ──────────────────────────────────────────

    [Fact]
    public void Reset_ClearsActivePresetKey()
    {
        var svc = ServiceWithPreset("spike");

        svc.Reset();

        svc.ActivePresetKey.Should().BeNull();
    }

    // ── ClearActivePreset ─────────────────────────────────────────────────────

    [Fact]
    public void ClearActivePreset_SetsKeyToNull()
    {
        var svc = ServiceWithPreset("poolLow");

        svc.ClearActivePreset();

        svc.ActivePresetKey.Should().BeNull();
    }

    // ── Manual field changes clear key (via ClearActivePreset) ────────────────

    [Fact]
    public void ApplyPreset_ThenClear_ThenApplyAgain_SetsNewKey()
    {
        var svc = ServiceWithPreset("normal");
        svc.ClearActivePreset();

        var spike = ScenarioPresets.RequestPresets.Single(p => p.Key == "spike");
        svc.ApplyPreset(spike);

        svc.ActivePresetKey.Should().Be("spike");
    }

    // ── OnChange event fires on state mutations ───────────────────────────────

    [Fact]
    public void SetUserType_FiresOnChange()
    {
        var svc = new AppStateService();
        var fired = false;
        svc.OnChange += () => fired = true;

        svc.SetUserType(UserType.Architect);

        fired.Should().BeTrue();
    }

    [Fact]
    public void SetMode_FiresOnChange()
    {
        var svc = new AppStateService();
        var fired = false;
        svc.OnChange += () => fired = true;

        svc.SetMode(SimulationMode.Agentic);

        fired.Should().BeTrue();
    }

    [Fact]
    public void Reset_FiresOnChange()
    {
        var svc = new AppStateService();
        var fired = false;
        svc.OnChange += () => fired = true;

        svc.Reset();

        fired.Should().BeTrue();
    }

    // ── TD-01: RunMultiCostCenter routes through service ──────────────────────

    [Fact]
    public void RunMultiCostCenter_StoresStateInProperty()
    {
        var svc = new AppStateService();
        var state = MultiCostCenterState.CreateDefault();

        svc.RunMultiCostCenter(state);

        svc.MultiCCState.Should().NotBeNull();
        svc.MultiCCState.Should().Be(state);
    }

    [Fact]
    public void RunMultiCostCenter_AccumulatesLogs()
    {
        var svc = new AppStateService();
        var state = MultiCostCenterState.CreateDefault();
        state.Logs.Add("old log");

        svc.RunMultiCostCenter(state);

        // Logs should accumulate, not be cleared — old log should still be there
        state.Logs.Should().Contain("old log");
        // Plus new logs from this run
        state.Logs.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void RunMultiCostCenter_ExecutesSimulation()
    {
        var svc = new AppStateService();
        var state = MultiCostCenterState.CreateDefault();

        svc.RunMultiCostCenter(state);

        // After execution, logs should be populated
        state.Logs.Should().NotBeEmpty();
    }

    [Fact]
    public void RunMultiCostCenter_FiresOnChange()
    {
        var svc = new AppStateService();
        var state = MultiCostCenterState.CreateDefault();
        var fired = false;
        svc.OnChange += () => fired = true;

        svc.RunMultiCostCenter(state);

        fired.Should().BeTrue();
    }

    [Fact]
    public void RunMultiCostCenter_WithNullState_ReturnsEarly()
    {
        var svc = new AppStateService();
        var fired = false;
        svc.OnChange += () => fired = true;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        svc.RunMultiCostCenter(null);
#pragma warning restore CS8625

        svc.MultiCCState.Should().BeNull();
        fired.Should().BeFalse("because null state should return early without notification");
    }
}
