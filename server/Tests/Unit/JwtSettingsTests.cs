using Domain.Exceptions;
using Domain.Settings;

namespace Unit;

public class JwtSettingsTests
{
    private static JwtSettings MakeValid() => new()
    {
        Secret = "supersecretkey_for_testing_32chars!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenLifetime = 15,
        RefreshTokenLifetime = 60
    };

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        var settings = MakeValid();
        var ex = Record.Exception(settings.Validate);
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSecretIsEmpty()
    {
        var settings = new JwtSettings
        {
            Secret = "",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenLifetime = 15,
            RefreshTokenLifetime = 60
        };
        Assert.Throws<ConfigurationFailureException>(settings.Validate);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSecretIsTooShort()
    {
        var settings = new JwtSettings
        {
            Secret = "tooshort",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenLifetime = 15,
            RefreshTokenLifetime = 60
        };
        Assert.Throws<ConfigurationFailureException>(settings.Validate);
    }
}