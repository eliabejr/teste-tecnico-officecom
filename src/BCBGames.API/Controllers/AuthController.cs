using BCBGames.API.Auth;
using DomainUser = BCBGames.Domain.Entities.User;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BCBGames.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;

    public AuthController(AppDbContext db, IUserRepository users, IJwtTokenService jwt)
    {
        _db = db;
        _users = users;
        _jwt = jwt;
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken, DateTime ExpiresAtUtc);

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        if (request.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var exists = await _users.ExistsByEmailAsync(request.Email, ct);
        if (exists) return Conflict("Email already registered.");

        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = DomainUser.Create(request.Email, passwordHash, role: "User");

        await _users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAtUtc) = _jwt.CreateAccessToken(user);
        return CreatedAtAction(nameof(Me), new { }, new AuthResponse(user.Id, user.Email, user.Role, token, expiresAtUtc));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null) return Unauthorized();

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var (token, expiresAtUtc) = _jwt.CreateAccessToken(user);
        return Ok(new AuthResponse(user.Id, user.Email, user.Role, token, expiresAtUtc));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var user = await _db.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Email, u.Role, u.CreatedAt })
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user is null ? Unauthorized() : Ok(user);
    }
}

