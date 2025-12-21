using BCBGames.API.Middleware;
using BCBGames.API.RateLimiting;
using BCBGames.API.Auth;
using BCBGames.Application.Behaviors;
using BCBGames.Application.Commands.CreateAccount;
using BCBGames.Infrastructure;
using BCBGames.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using DotNetEnv;
using System.Text;

var currentDir = Directory.GetCurrentDirectory();
var envPaths = new[]
{
    Path.Combine(currentDir, ".env"),
    Path.Combine(currentDir, "..", "..", ".env"),
    Path.Combine(currentDir, "..", "..", "..", ".env")
};

foreach (var envPath in envPaths)
{
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        break;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BCB Games API",
        Version = "v1",
        Description = "Documentação da API do BCB Games"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization Bearer. Ex: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateAccountCommand).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

var redisConnectionString = builder.Configuration.GetSection("Redis:ConnectionString").Value
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddRedis(redisConnectionString, name: "redis");

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RateLimitingMiddleware>();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("aplicando migrationns...");
    db.Database.Migrate();
    logger.LogInformation("migrations aplicadas com sucesso.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Ocorreu um erro ao aplicar as migrations.");
    throw;
}

app.Run();

public partial class Program { }
