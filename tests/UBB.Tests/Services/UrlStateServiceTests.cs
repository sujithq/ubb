using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Services;

/// <summary>
/// Plain xUnit tests for UrlStateService serialization.
/// Covers TD-09: deserialization must not silently swallow errors — tested here
/// by verifying it returns null (not throws) on malformed input.
/// The Console.Error logging is a side-effect not asserted here.
/// </summary>
public class UrlStateServiceTests
{
    private readonly UrlStateService _svc = new();

    // ── Serialize → Deserialize round-trip ───────────────────────────────────

    [Fact]
    public void SerializeAppState_ProducesUrlSafeString()
    {
        var encoded = _svc.SerializeAppState(new RequestFlowState());

        // Base64Url must not contain +, /, or = (standard Base64 chars replaced for URL safety)
        encoded.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    [Fact]
    public void SerializeFlowState_ProducesUrlSafeString()
    {
        var encoded = _svc.SerializeFlowState(new RequestFlowState());

        encoded.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    [Fact]
    public void DeserializeAppState_DoesNotThrow_WhenInputIsTruncated()
    {
        var encoded   = _svc.SerializeAppState(new RequestFlowState());
        var truncated = encoded[..^5]; // cut last 5 chars

        // May parse partially or fail — must not throw either way
        var act = () => _svc.DeserializeAppState(truncated);
        act.Should().NotThrow();
    }

    // ── TD-03: SerializeAppState & DeserializeAppState (Flow + Multi-CC) ─────

    [Fact]
    public void SerializeAppState_RoundTrip_WithFlowStateOnly()
    {
        var flowState = new RequestFlowState
        {
            UserType = UserType.Standard,
            Mode = SimulationMode.Single,
            UniversalLimitCredits = 1000,
            UserUsedCredits = 250,
        };

        var encoded = _svc.SerializeAppState(flowState, multiCCState: null, activePresetKey: "test-preset", mode: SimulationMode.Single);
        var (restoredFlow, restoredMultiCC, presetKey, mode) = _svc.DeserializeAppState(encoded);

        restoredFlow.Should().NotBeNull();
        restoredFlow!.UserType.Should().Be(UserType.Standard);
        restoredFlow.UniversalLimitCredits.Should().Be(1000);
        restoredFlow.UserUsedCredits.Should().Be(250);
        restoredMultiCC.Should().BeNull();
        presetKey.Should().Be("test-preset");
        mode.Should().Be(SimulationMode.Single);
    }

    [Fact]
    public void SerializeAppState_RoundTrip_WithMultiCCState()
    {
        var flowState = new RequestFlowState
        {
            UserType = UserType.Architect,
            Mode = SimulationMode.MultiCostCenter,
            UniversalLimitCredits = 5000,
        };
        var multiCCState = MultiCostCenterState.CreateDefault();

        var encoded = _svc.SerializeAppState(flowState, multiCCState, activePresetKey: null, mode: SimulationMode.MultiCostCenter);
        var (restoredFlow, restoredMultiCC, presetKey, mode) = _svc.DeserializeAppState(encoded);

        restoredFlow.Should().NotBeNull();
        restoredFlow!.Mode.Should().Be(SimulationMode.MultiCostCenter);
        restoredMultiCC.Should().NotBeNull();
        restoredMultiCC!.CostCenters.Should().HaveCount(3);
        restoredMultiCC.CostCenters[0].Name.Should().Be("Engineering");
        restoredMultiCC.PoolRemainingCredits.Should().Be(390_000);
        mode.Should().Be(SimulationMode.MultiCostCenter);
    }

    [Fact]
    public void SerializeFlowState_RoundTrip_WithPreset()
    {
        var flowState = new RequestFlowState
        {
            UserType = UserType.Standard,
            SingleRequestCredits = 200,
        };

        var encoded = _svc.SerializeFlowState(flowState, activePresetKey: "preset-123");
        var (restoredFlow, presetKey) = _svc.DeserializeFlowState(encoded);

        restoredFlow.Should().NotBeNull();
        restoredFlow!.SingleRequestCredits.Should().Be(200);
        presetKey.Should().Be("preset-123");
    }

    // ── TD-09: DeserializeAppState returns null (not throws) on bad input ─────

    [Fact]
    public void DeserializeAppState_ReturnsNullFlowState_WhenInputIsGarbage()
    {
        var act = () => _svc.DeserializeAppState("!!!not-base64!!!");

        act.Should().NotThrow();
        var (flow, multiCC, presetKey, mode) = _svc.DeserializeAppState("!!!not-base64!!!");
        flow.Should().BeNull();
        multiCC.Should().BeNull();
        presetKey.Should().BeNull();
        mode.Should().Be(SimulationMode.Single); // default fallback
    }

    [Fact]
    public void DeserializeAppState_ReturnsNullFlowState_WhenInputIsValidBase64ButNotJson()
    {
        var notJson = Convert.ToBase64String("not json"u8.ToArray())
                             .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var act = () => _svc.DeserializeAppState(notJson);

        act.Should().NotThrow();
        var (flow, _, _, _) = _svc.DeserializeAppState(notJson);
        flow.Should().BeNull();
    }

    // ── TD-09: DeserializeFlowState returns null (not throws) on bad input ────

    [Fact]
    public void DeserializeFlowState_ReturnsNullState_WhenInputIsGarbage()
    {
        var act = () => _svc.DeserializeFlowState("!!!not-base64!!!");

        act.Should().NotThrow();
        var (flow, presetKey) = _svc.DeserializeFlowState("!!!not-base64!!!");
        flow.Should().BeNull();
        presetKey.Should().BeNull();
    }

    [Fact]
    public void DeserializeFlowState_ReturnsNullState_WhenInputIsValidBase64ButNotJson()
    {
        var notJson = Convert.ToBase64String("not json"u8.ToArray())
                             .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var act = () => _svc.DeserializeFlowState(notJson);

        act.Should().NotThrow();
        var (flow, _) = _svc.DeserializeFlowState(notJson);
        flow.Should().BeNull();
    }
}

