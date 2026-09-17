using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicDistribution.Api.Middleware;
using MusicDistribution.Application.Interfaces;
using MusicDistribution.Application.Services;
using MusicDistribution.Infrastructure.Persistence;
using MusicDistribution.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Wrap the default [ApiController] auto-validation (400) response in the
        // same envelope shape we use for manually-thrown errors, for a consistent API.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage))
                .ToList();

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                message = "One or more validation errors occurred.",
                errors
            });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Music Distribution API",
        Version = "v1",
        Description = "Track management API for artists, tracks and DSP distributions."
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter a JWT token obtained from POST /api/auth/login."
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<ITrackService, TrackService>();

// Jwt:Key is intentionally blank in appsettings.json - it must be supplied via
// `dotnet user-secrets` (local dev) or an environment variable (anywhere else).
// See README.md "Configuring secrets" for the exact commands. Failing fast here
// with a clear message beats either a NullReferenceException later or, worse,
// silently signing tokens with an empty/weak key.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it with " +
        "'dotnet user-secrets set \"Jwt:Key\" \"<a long random string>\"' " +
        "(run from src/MusicDistribution.Api) or the Jwt__Key environment variable. " +
        "See README.md for details.");
}
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is too short (must be at least 32 characters for HMAC-SHA256). " +
        "See README.md for how to configure it.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Pipeline ----------

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply pending EF Core migrations and seed sample data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();

// Exposed for WebApplicationFactory-based integration testing.
public partial class Program { }
