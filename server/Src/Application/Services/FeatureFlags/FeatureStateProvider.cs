using Application.Common.Interfaces.Services;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;

namespace Application.Services.FeatureFlags;

public class FeatureStateProvider : IFeatureStateProvider
{
    private readonly EdgeFeatureHubConfig? _config;

    public FeatureStateProvider()
    {
        try
        {
            var config = new EdgeFeatureHubConfig(
                "http://localhost:8085",
                "5f184206-7bd1-4dda-a1c3-c5b811d312f5/WQIqKZDCQ9KeEHRYwM8tmtVApR5vbeO8nysmeR5i"
            );
            if (config.Init().Wait(TimeSpan.FromSeconds(5)))
                _config = config;
            else
                Console.WriteLine("[FeatureStateProvider] Connection timed out — running without feature flags.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FeatureStateProvider] Failed to connect: {ex.Message}");
        }
    }
    
    public bool IsEnabled(string featureKey)
    {
        if (_config == null) return true;
    
        var feature = _config.Repository[featureKey];
        if (feature == null || feature.Value == null) return true;
    
        return (bool)feature.Value;
    }
}

