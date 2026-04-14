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
                "be4ae685-6bfd-407d-9323-8f0c103ec2ce/LYjxcSN52vnN2gfqvouuusOC4vmqIy4eoYaj6YLT"
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

