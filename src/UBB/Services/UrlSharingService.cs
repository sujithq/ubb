using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UBB.Services;

/// <summary>
/// Coordinates URL-based state sharing: builds share links (with clipboard + toast)
/// and restores app state from the URL hash on startup.
/// Extracted from Home.razor (TD-15) — pure serialization stays in UrlStateService.
/// </summary>
public class UrlSharingService(
    AppStateService state,
    UrlStateService urlService,
    NavigationManager nav,
    IJSRuntime js)
{
    /// <summary>
    /// Reads the URL hash and restores app state from it (new format first,
    /// legacy flow-state format as fallback). Shows a warning toast when a hash
    /// is present but unreadable. Returns true if state was restored.
    /// </summary>
    public async Task<bool> TryRestoreFromUrlAsync()
    {
        try
        {
            var hash = await js.InvokeAsync<string>("ubb.getHash");
            if (string.IsNullOrEmpty(hash) || !hash.StartsWith('#'))
                return false;

            var encoded = hash[1..];
            if (string.IsNullOrEmpty(encoded))
                return false;

            // Try the full app-state format first (flow + multi-CC + preset + mode)
            var (flowState, multiCCState, presetKey, mode) = urlService.DeserializeAppState(encoded);
            if (flowState != null)
            {
                state.LoadFlowStateWithoutNotifying(flowState);
                state.SetModeWithoutNotifying(mode); // AFTER flow state, so the mode survives
                if (multiCCState != null)
                    state.LoadMultiCCStateWithoutNotifying(multiCCState);
                if (!string.IsNullOrEmpty(presetKey))
                    state.ActivePresetKey = presetKey;
                return true;
            }

            // Legacy flow-state format (TD-02 backward compatibility)
            var (legacyFlowState, legacyPresetKey) = urlService.DeserializeFlowState(encoded);
            if (legacyFlowState != null)
            {
                state.LoadFlowStateWithoutNotifying(legacyFlowState);
                if (!string.IsNullOrEmpty(legacyPresetKey))
                    state.ActivePresetKey = legacyPresetKey;
                return true;
            }

            // Hash present but unreadable in both formats
            await js.InvokeVoidAsync("ubb.showToast", "⚠️ Could not restore shared state — the link may be invalid or incomplete.");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UrlSharingService] Failed to restore state from URL: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Serializes the complete app state into the URL bar, copies the link
    /// to the clipboard, and shows a confirmation toast.
    /// </summary>
    public async Task ShareAsync()
    {
        var encoded = urlService.SerializeAppState(
            state.FlowState,
            state.MultiCCState,
            state.ActivePresetKey,
            state.FlowState.Mode);
        var baseUri = nav.Uri.Split('#')[0];
        var shareUrl = $"{baseUri}#{encoded}";

        nav.NavigateTo(shareUrl, forceLoad: false);

        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", shareUrl);
            await js.InvokeVoidAsync("ubb.showToast", "Share link copied to clipboard!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UrlSharingService] Failed to copy to clipboard: {ex.Message}");
        }
    }
}
