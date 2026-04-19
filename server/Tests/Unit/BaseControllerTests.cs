using System.Security.Claims;
using Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Unit;

public class BaseControllerTests
{
    private sealed class TestController : BaseController
    {
        public Guid CallGetUserId() => GetUserId();
    }

    private static TestController MakeController(ClaimsPrincipal user)
    {
        var controller = new TestController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    private static ClaimsPrincipal AuthenticatedUser(string? nameIdentifier)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser")
        };
        if (nameIdentifier != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void GetUserId_ShouldReturnGuid_WhenUserIsAuthenticated()
    {
        var id = Guid.NewGuid();
        var controller = MakeController(AuthenticatedUser(id.ToString()));

        var result = controller.CallGetUserId();

        Assert.Equal(id, result);
    }

    [Fact]
    public void GetUserId_ShouldThrow_WhenUserIsNotAuthenticated()
    {
        var controller = MakeController(new ClaimsPrincipal(new ClaimsIdentity()));
        Assert.Throws<UnauthorizedAccessException>(() => controller.CallGetUserId());
    }

    [Fact]
    public void GetUserId_ShouldThrow_WhenNameIdentifierClaimIsMissing()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "testuser")], "TestAuth");
        var controller = MakeController(new ClaimsPrincipal(identity));
        Assert.Throws<UnauthorizedAccessException>(() => controller.CallGetUserId());
    }

    [Fact]
    public void GetUserId_ShouldThrow_WhenNameIdentifierIsNotAValidGuid()
    {
        var controller = MakeController(AuthenticatedUser("not-a-guid"));
        Assert.Throws<UnauthorizedAccessException>(() => controller.CallGetUserId());
    }
}