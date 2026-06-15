using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.Grpc;
using PersonalisationEngine.Api.Middleware;
using PersonalisationEngine.Api.Services;
using PersonalisationEngine.Api.Services.Claude;
using PersonalisationEngine.Api.Services.Rules;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5080, o => o.Protocols = HttpProtocols.Http1);
    options.ListenLocalhost(5081, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Host.UseSerilog((ctx, services, config) =>
    config.ReadFrom.Configuration(ctx.Configuration)
          .ReadFrom.Services(services)
          .WriteTo.Console(outputTemplate:
              "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Load .env.local for local development (keys use __ as config section separator)
var envLocal = Path.Combine(builder.Environment.ContentRootPath, ".env.local");
if (File.Exists(envLocal))
{
    var overrides = File.ReadAllLines(envLocal)
        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'))
        .Select(l => l.Split('=', 2))
        .Where(p => p.Length == 2)
        .ToDictionary(
            p => p[0].Trim().Replace("__", ":"),
            p => (string?)p[1].Trim());
    builder.Configuration.AddInMemoryCollection(overrides);
}

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS for Vite dev server
builder.Services.AddCors(options =>
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()));

// Typed HTTP client for Anthropic API
builder.Services.AddHttpClient<IClaudeClient, ClaudeClient>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    var apiKey = builder.Configuration["Anthropic:ApiKey"] ?? "";
    if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "REPLACE_ME")
    {
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }
});

// Seed demo players in Development only
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<DevDataSeeder>();

// API key authentication — validates X-Api-Key header against Auth:ApiKey config
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
               ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IRulesEngine, RulesEngine>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Controllers with camelCase JSON and string enums
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Personalisation Engine API", Version = "v1" }));

var app = builder.Build();

// Migrate on startup only when explicitly enabled (Development default: true, all others: false)
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("ViteDev");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<EventIngestService>();
app.MapControllers();

app.Run();

public partial class Program { }
