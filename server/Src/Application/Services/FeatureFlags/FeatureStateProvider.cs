using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;

namespace Application.Services.FeatureFlags;

public class FeatureStateProvider
{
    private readonly EdgeFeatureHubConfig _config;

    public FeatureStateProvider()
    {
        var config = new EdgeFeatureHubConfig("http://localhost:8085", "be4ae685-6bfd-407d-9323-8f0c103ec2ce/LYjxcSN52vnN2gfqvouuusOC4vmqIy4eoYaj6YLT");
        config.Init().Wait();
        _config = config;
    }
    
    public bool IsEnabled(string featureKey)
    {
        return (bool)_config.Repository[featureKey].Value;
    }
}

