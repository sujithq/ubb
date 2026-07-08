using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UBB.Components;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Components;

/// <summary>
/// bUnit component tests for PresetPanel.
/// Verifies the active-preset highlighting introduced to fix TD-10.
/// </summary>
public class PresetPanelTests : TestContext
{
    public PresetPanelTests()
    {
        // PresetPanel uses ScenarioPresets as a static class — no DI needed.
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    [Fact]
    public void PresetPanel_RendersAllRequestPresets()
    {
        var cut = RenderComponent<PresetPanel>();

        var buttons = cut.FindAll("button.ubb-preset-btn");
        buttons.Count.Should().Be(ScenarioPresets.RequestPresets.Count);
    }

    [Fact]
    public void PresetPanel_ShowsEachPresetLabel()
    {
        var cut = RenderComponent<PresetPanel>();

        foreach (var preset in ScenarioPresets.RequestPresets)
            cut.Markup.Should().Contain(preset.Label);
    }

    // ── Active preset highlighting ────────────────────────────────────────────

    [Fact]
    public void PresetPanel_WhenActiveKeySet_HighlightsMatchingButton()
    {
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.ActivePresetKey, "normal"));

        var activeButtons = cut.FindAll("button.ubb-preset-btn--active");
        activeButtons.Count.Should().Be(1);
    }

    [Fact]
    public void PresetPanel_WhenActiveKeySet_ShowsCheckmarkOnActivePreset()
    {
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.ActivePresetKey, "spike"));

        var activeBtn = cut.Find("button.ubb-preset-btn--active");
        activeBtn.TextContent.Should().StartWith("✓");
    }

    [Fact]
    public void PresetPanel_WhenNoActiveKey_NoButtonHighlighted()
    {
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.ActivePresetKey, (string?)null));

        var activeButtons = cut.FindAll("button.ubb-preset-btn--active");
        activeButtons.Count.Should().Be(0);
    }

    [Fact]
    public void PresetPanel_WhenActiveKeyChanges_UpdatesHighlight()
    {
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.ActivePresetKey, "normal"));

        cut.SetParametersAndRender(p => p.Add(x => x.ActivePresetKey, "spike"));

        var activeBtn = cut.Find("button.ubb-preset-btn--active .ubb-preset-title");
        activeBtn.TextContent.Should().Contain("Power user spike");
    }

    [Fact]
    public void PresetPanel_WhenActiveKeyCleared_RemovesAllHighlights()
    {
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.ActivePresetKey, "normal"));

        cut.SetParametersAndRender(p => p.Add(x => x.ActivePresetKey, (string?)null));

        cut.FindAll("button.ubb-preset-btn--active").Count.Should().Be(0);
    }

    // ── Click callback ────────────────────────────────────────────────────────

    [Fact]
    public void PresetPanel_WhenButtonClicked_InvokesCallbackWithCorrectPreset()
    {
        RequestPreset? received = null;
        var cut = RenderComponent<PresetPanel>(p => p
            .Add(x => x.OnPresetSelected, (RequestPreset r) => received = r));

        cut.Find("button.ubb-preset-btn").Click();

        received.Should().NotBeNull();
        received!.Key.Should().Be(ScenarioPresets.RequestPresets[0].Key);
    }
}
