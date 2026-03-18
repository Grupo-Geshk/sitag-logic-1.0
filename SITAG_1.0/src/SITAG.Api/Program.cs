using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SITAG.Api;
using SITAG.Api.Middleware;
using SITAG.Application;
using SITAG.Infrastructure;

// ── Serilog bootstrap (before CreateBuilder) ─────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    // ── .env loader (Development only) ───────────────────────────────────────
    var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    if (aspnetEnv == "Development")
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        DotEnvLoader.Load(Path.Combine(projectDir, ".env"));
    }

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog full pipeline ─────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    });

    // ── Railway port binding ──────────────────────────────────────────────────
    var railwayPort = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(railwayPort))
        builder.WebHost.UseUrls($"http://+:{railwayPort}");

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = (builder.Configuration["CORS:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtIssuer   = builder.Configuration["JWT:Issuer"]    ?? throw new InvalidOperationException("JWT__Issuer is not set.");
    var jwtAudience = builder.Configuration["JWT:Audience"]  ?? throw new InvalidOperationException("JWT__Audience is not set.");
    var jwtKey      = builder.Configuration["JWT:SigningKey"] ?? throw new InvalidOperationException("JWT__SigningKey is not set.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false; // keep claim names as written in the token (e.g. "sub" stays "sub")
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtIssuer,
                ValidAudience            = jwtAudience,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew                = TimeSpan.FromSeconds(30),
                RoleClaimType            = "role",
            };
        });

    builder.Services.AddAuthorization();

    // ── Rate limiting (REQ-INFRA-07) ──────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // 200 req/min per authenticated tenant (keyed by tenantId claim)
        options.AddPolicy("tenant", httpContext =>
        {
            var tenantId = httpContext.User.FindFirstValue("tenantId") ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(tenantId, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit       = 200,
                    Window            = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit        = 0,
                });
        });

        // 20 req/min per IP for auth endpoints
        options.AddPolicy("auth", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit       = 20,
                    Window            = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit        = 0,
                });
        });
    });

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Swagger (Development only) ────────────────────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "SITAG API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In           = ParameterLocation.Header,
                Description  = "Paste the JWT access token: Bearer {token}",
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
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
    }

    // ── App pipeline ──────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Seed bootstrap accounts ───────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SITAG.Infrastructure.Persistence.SitagDbContext>();
        await SITAG.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SITAG API v1"));
    }

    app.UseCors();
    // HTTPS redirect only in local development — Railway terminates TLS at the edge
    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseMiddleware<TenantStatusMiddleware>();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Apply rate-limit policies: "auth" on auth routes, "tenant" everywhere else
    app.MapControllers()
       .RequireRateLimiting("tenant");

    // Auth routes get the stricter IP-based limit instead
    // (applied via [EnableRateLimiting] attribute on AuthController)

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
