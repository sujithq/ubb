using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using UBB.Models;

namespace UBB.Services;

public class UrlStateService
{
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
        catch
        {
            return null;
        }
    }

    public void PushToUrl(NavigationManager nav, SimulationConfig config)
    {
        var encoded = Serialize(config);
        var baseUri = nav.Uri.Split('#')[0];
        nav.NavigateTo($"{baseUri}#{encoded}", forceLoad: false);
    }

    public SimulationConfig? LoadFromUrl(NavigationManager nav)
    {
        var hashIndex = nav.Uri.IndexOf('#');
        if (hashIndex < 0 || hashIndex >= nav.Uri.Length - 1)
            return null;
        return Deserialize(nav.Uri[(hashIndex + 1)..]);
    }
}
