using Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/features")]
public class FeatureFlagsRegisterController(IFeatureStateProvider featureStateProvider) : ControllerBase
{
    [HttpGet("register-user")]
    public IActionResult GetRegisterUserFlag()
    {
        var enabled = featureStateProvider.IsEnabled("RegisterNewUsers");
        return Ok(new { enabled });
    }
}