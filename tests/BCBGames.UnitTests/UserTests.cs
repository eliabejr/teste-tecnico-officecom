using BCBGames.Domain.Entities;

namespace BCBGames.UnitTests;

public class UserTests
{
    [Fact]
    public void Create_WhenEmailIsNullOrWhitespace_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create("  ", "hash"));
        Assert.Equal("email", ex.ParamName);
        Assert.Contains("Email is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WhenPasswordHashIsNullOrWhitespace_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create("test@bcbgames", " "));
        Assert.Equal("passwordHash", ex.ParamName);
        Assert.Contains("PasswordHash is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldNormalizeEmail_AndInitializeDefaults()
    {
        var nowBefore = DateTime.UtcNow;

        var user = User.Create("  TEST@Bcbgames.COM  ", "hash");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("test@bcbgames.com", user.Email);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("User", user.Role);
        Assert.True(user.CreatedAt >= nowBefore);
        Assert.NotNull(user.Accounts);
        Assert.Empty(user.Accounts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenRoleIsWhitespace_ShouldDefaultToUser(string role)
    {
        var user = User.Create("test@bcbgames", "hash", role);
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public void Create_WhenRoleHasValue_ShouldTrim()
    {
        var user = User.Create("test@bcbgames", "hash", " Admin ");
        Assert.Equal("Admin", user.Role);
    }
}

