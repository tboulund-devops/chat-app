namespace Application.Common.Interfaces.Services;

public interface IFeatureStateProvider
{
    bool IsEnabled(string featureKey);
}