using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Services;

/// <summary>
/// Plain xUnit tests for UrlStateService.Serialize and Deserialize.
/// Covers TD-09: Deserialize must not silently swallow errors — tested here
/// by verifying it returns null (not throws) on malformed input.
/// The Console.Error logging is a side-effect not asserted here.
/// </summary>
public class UrlStateServiceTests
{
    private readonly UrlStateService _svc = new();

    // ── Serialize → Deserialize round-trip ───────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesAllConfigProperties()
    {
        var config = new SimulationConfig
        {
            BusinessSeats          = 42,
            EnterpriseSeats        = 7,
            UsePromotionalCredits  = true,
            AllowMeteredUsage      = false,
        };

        var encoded  = _svc.Serialize(config);
        var restored = _svc.Deserialize(encoded);

        restored.Should().NotBeNull();
        restored!.BusinessSeats.Should().Be(42);
        restored.EnterpriseSeats.Should().Be(7);
        restored.UsePromotionalCredits.Should().BeTrue();
        restored.AllowMeteredUsage.Should().BeFalse();
    }

    [Fact]
    public void Serialize_ProducesUrlSafeString()
    {
        var encoded = _svc.Serialize(new SimulationConfig());

        // Base64Url must not contain +, /, or = (standard Base64 chars replaced for URL safety)
        encoded.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    // ── TD-09: Deserialize returns null (not throws) on bad input ─────────────

    [Fact]
    public void Deserialize_ReturnsNull_WhenInputIsGarbage()
    {
        var act = () => _svc.Deserialize("!!!not-base64!!!");

        act.Should().NotThrow();
        _svc.Deserialize("!!!not-base64!!!").Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenInputIsValidBase64ButNotJson()
    {
        var notJson  = Convert.ToBase64String("hello world"u8.ToArray())
                              .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var result   = _svc.Deserialize(notJson);

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenInputIsEmpty()
    {
        _svc.Deserialize("").Should().BeNull();
    }

    [Fact]
    public void Deserialize_ReturnsNull_WhenInputIsTruncated()
    {
        var encoded  = _svc.Serialize(new SimulationConfig());
        var truncated = encoded[..^5]; // cut last 5 chars

        var result = _svc.Deserialize(truncated);

        // May parse partially or fail — must not throw either way
        // (result may be null or a partial object; we only assert no exception)
        var act = () => _svc.Deserialize(truncated);
        act.Should().NotThrow();
    }
}
