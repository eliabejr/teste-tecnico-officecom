using BCBGames.API.Middleware;
using BCBGames.Application.Behaviors;
using BCBGames.Application.Commands.CreateAccount;
using BCBGames.Infrastructure;
using BCBGames.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
});

builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

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
