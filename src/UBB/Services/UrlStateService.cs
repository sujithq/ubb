using System.Text;
using System.Text.Json;
using UBB.Models;

namespace UBB.Services;

/// <summary>Wrapper for URL serialization that includes both state and active preset key.</summary>
public class UrlFlowStateSnapshot
{
    public RequestFlowState FlowState { get; set; } = new();
    public string? ActivePresetKey { get; set; }
}

/// <summary>Comprehensive wrapper for URL serialization: single-request flow, multi-CC state, preset, and mode.</summary>
public class UrlAppStateSnapshot
{
    public RequestFlowState FlowState { get; set; } = new();
    public MultiCostCenterState? MultiCCState { get; set; }
    public string? ActivePresetKey { get; set; }
    public SimulationMode Mode { get; set; } = SimulationMode.Single;
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

    /// <summary>Serialize complete app state: flow + multi-CC + preset + mode.</summary>
    public string SerializeAppState(
        RequestFlowState flowState, 
        MultiCostCenterState? multiCCState = null, 
        string? activePresetKey = null,
        SimulationMode mode = SimulationMode.Single)
    {
        var snapshot = new UrlAppStateSnapshot
        {
            FlowState = flowState,
            MultiCCState = multiCCState,
            ActivePresetKey = activePresetKey,
            Mode = mode
        };
        var json = JsonSerializer.Serialize(snapshot);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>Deserialize complete app state. Returns (flowState, multiCCState, presetKey, mode).</summary>
    public (RequestFlowState? FlowState, MultiCostCenterState? MultiCCState, string? PresetKey, SimulationMode Mode) DeserializeAppState(string encoded)
    {
        try
        {
            var padded = encoded.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var snapshot = JsonSerializer.Deserialize<UrlAppStateSnapshot>(json);
            return (snapshot?.FlowState, snapshot?.MultiCCState, snapshot?.ActivePresetKey, snapshot?.Mode ?? SimulationMode.Single);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UrlStateService] Failed to deserialize app state: {ex.Message}");
            return (null, null, null, SimulationMode.Single);
        }
    }
}
