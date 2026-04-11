using Application.Common.Interfaces.Services;

namespace Integration.Fixtures;

public sealed class AlwaysEnabledFeatureStateProvider : IFeatureStateProvider
{
    public bool IsEnabled(string featureKey) => true;   
}