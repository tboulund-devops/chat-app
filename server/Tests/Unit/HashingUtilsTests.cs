using Infrastructure.Utils;
namespace Unit;

[Collection("Utils Collection")]
public class HashingUtilsTests
{
    private readonly HashingUtils _hashingUtils = new();
    
    [Fact]
    public Task ShouldCreatePasswordHash()
    {
        // Arrange - create the REAL implementation
        const string password = "password";

        // Act - call the real methods
        _hashingUtils.CreatePasswordHash(password, out var passwordHash);

        // Assert - verify the real implementation works
        Assert.NotNull(passwordHash);
        
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldVerifySuccessfullyPasswordHash()
    {
        // Arrange
        const string password = "veryNiceAndLongPassword";
        _hashingUtils.CreatePasswordHash(password, out var hash);
        
        // Act & Assert
        Assert.True(_hashingUtils.VerifyPasswordHash(password, hash));
        
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldFailPasswordHashVerification()
    {
        // Arrange
        const string wrongPassword = "wrongPassword";
        _hashingUtils.CreatePasswordHash("correctPassword", out var hash);
        
        // Act & Assert
        Assert.False(_hashingUtils.VerifyPasswordHash(wrongPassword, hash));
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldGenerateRefreshTokenBase64String()
    {
        // Arrange
        var token = _hashingUtils.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        
        // Act & Assert
        Assert.NotNull(token);
        Assert.Equal(64, bytes.Length); // Check if the token is 64 bytes long
        return Task.CompletedTask;
    }
    
}