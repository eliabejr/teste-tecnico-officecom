using System.ComponentModel.DataAnnotations;

namespace BCBGames.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }

    [MaxLength(320)]
    public string Email { get; private set; } = string.Empty;

    [MaxLength(500)]
    public string PasswordHash { get; private set; } = string.Empty;

    [MaxLength(50)]
    public string Role { get; private set; } = "User";

    public DateTime CreatedAt { get; private set; }

    public ICollection<Account> Accounts { get; private set; } = new List<Account>();

    private User() { }

    public static User Create(string email, string passwordHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(role))
            role = "User";

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

