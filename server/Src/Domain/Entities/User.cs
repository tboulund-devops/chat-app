using Domain.Enums;

namespace Domain.Entities;

public sealed record User
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public RoleType Role { get; set; } = RoleType.User;
    
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.UnixEpoch);

    public required string Email { get; set; }
    public byte[] PasswordHash { get; set; } = null!;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpires { get; set; } 
    
    public bool Activated { get; set; } = false;
    public DateTime ExpireDate { get; set; } 
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public static User Create(
        string firstName,
        string lastName,
        string email,
        byte[] passwordHash,
        RoleType role = RoleType.User)
    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}