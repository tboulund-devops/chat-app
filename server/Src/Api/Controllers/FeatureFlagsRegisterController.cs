using Application.Services.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/features")]
public class FeatureFlagsRegisterController(FeatureStateProvider featureStateProvider) : ControllerBase
{
    [HttpGet("register-user")]
    public IActionResult GetRegisterUserFlag()
    {
        var enabled = featureStateProvider.IsEnabled("RegisterNewUsers");
        return Ok(new { enabled });
    }
}