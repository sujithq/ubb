using Bunit;
using FluentAssertions;
using UBB.Components;
using UBB.Models;

namespace UBB.Tests.Components;

/// <summary>
/// bUnit component tests for StatCards — verifies live value display.
/// Uses element selectors so tests are culture-agnostic (N0 formatting
/// varies by OS locale — "1,500" vs "1.500" vs "1 500").
/// </summary>
public class StatCardsTests : TestContext
{
    // Mirror StatCards.Fmt so expected values always match rendered values.
    private static string Fmt(decimal value) =>
        Math.Max(0, Math.Round(value)).ToString("N0");

    private static RequestFlowState DefaultState() => new()
    {
        UserUsedCredits                   = 1_500,
        PoolRemainingCredits              = 388_500,
        CostCenterMeteredRemainingCredits = 200_000,
        EnterpriseMeteredRemainingCredits = 1_000_000,
    };

    // ── StatCard count ────────────────────────────────────────────────────────

    [Fact]
    public void StatCards_RendersFourCards()
    {
        var cut = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, DefaultState()));

        cut.FindAll(".ubb-stat-card").Count.Should().Be(4);
    }

    // ── Value display uses culture-matched Fmt ────────────────────────────────

    [Fact]
    public void StatCards_DisplaysUserUsedCredits()
    {
        var state = DefaultState();
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(state.UserUsedCredits));
    }

    [Fact]
    public void StatCards_DisplaysPoolRemainingCredits()
    {
        var state = DefaultState();
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(state.PoolRemainingCredits));
    }

    [Fact]
    public void StatCards_DisplaysCostCenterCredits()
    {
        var state = DefaultState();
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(state.CostCenterMeteredRemainingCredits));
    }

    [Fact]
    public void StatCards_DisplaysEnterpriseCapCredits()
    {
        var state = DefaultState();
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(state.EnterpriseMeteredRemainingCredits));
    }

    // ── Re-render on state change ─────────────────────────────────────────────

    [Fact]
    public void StatCards_WhenValueUpdated_ReRendersWithNewValue()
    {
        var state = DefaultState();
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        state.UserUsedCredits = 9_999;
        cut.SetParametersAndRender(p => p.Add(x => x.FlowState, state));

        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(9_999));
    }

    // ── Negative clamping ─────────────────────────────────────────────────────

    [Fact]
    public void StatCards_ClampsNegativeValuesToZero()
    {
        var state = new RequestFlowState { PoolRemainingCredits = -100 };
        var cut   = RenderComponent<StatCards>(p => p.Add(x => x.FlowState, state));

        // Fmt clamps to Max(0, ...) — pool card should render "0" not "-100"
        var values = cut.FindAll(".ubb-stat-value").Select(e => e.TextContent).ToList();
        values.Should().Contain(Fmt(0));
        // The raw negative number must not appear anywhere in the rendered output
        cut.Markup.Should().NotContain("-100");
    }
}
