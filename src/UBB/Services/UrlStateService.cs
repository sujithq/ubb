using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using UBB.Models;

namespace UBB.Services;

/// <summary>Wrapper for URL serialization that includes both state and active preset key.</summary>
public class UrlFlowStateSnapshot
{
    public RequestFlowState FlowState { get; set; } = new();
    public string? ActivePresetKey { get; set; }
}

public class UrlStateService
{
    public string SerializeFlowState(RequestFlowState state, string? activePresetKey = null)
    {
        var snapshot = new UrlFlowStateSnapshot 
        { 
            FlowState = state,
            ActivePresetKey = activePresetKey
        };
        var json = JsonSerializer.Serialize(snapshot);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public (RequestFlowState? State, string? PresetKey) DeserializeFlowState(string encoded)
    {
        try
        {
            var padded = encoded.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var snapshot = JsonSerializer.Deserialize<UrlFlowStateSnapshot>(json);
            return (snapshot?.FlowState, snapshot?.ActivePresetKey);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UrlStateService] Failed to deserialize flow state: {ex.Message}");
            return (null, null);
        }
    }

    public string Serialize(SimulationConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public SimulationConfig? Deserialize(string encoded)
    {
        try
        {
            var padded = encoded.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return JsonSerializer.Deserialize<SimulationConfig>(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UrlStateService] Failed to deserialize URL state: {ex.Message}");
            return null;
        }
    }

    public void PushToUrl(NavigationManager nav, SimulationConfig config)
    {
        var encoded = Serialize(config);
        var baseUri = nav.Uri.Split('#')[0];
        nav.NavigateTo($"{baseUri}#{encoded}", forceLoad: false);
    }

    public void PushFlowStateToUrl(NavigationManager nav, RequestFlowState state, string? activePresetKey = null)
    {
        var encoded = SerializeFlowState(state, activePresetKey);
        var baseUri = nav.Uri.Split('#')[0];
        nav.NavigateTo($"{baseUri}#{encoded}", forceLoad: false);
    }

    public (RequestFlowState? State, string? PresetKey) LoadFlowStateFromUrl(NavigationManager nav)
    {
        var hashIndex = nav.Uri.IndexOf('#');
        if (hashIndex < 0 || hashIndex >= nav.Uri.Length - 1)
            return (null, null);
        return DeserializeFlowState(nav.Uri[(hashIndex + 1)..]);
    }
}
