using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using AvailabilityService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AvailabilityOptions>(builder.Configuration.GetSection("Availability"));
builder.Services.AddSingleton<IAvailabilitySlotService, AvailabilitySlotService>();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(defaultConnection));
    builder.Services.AddSingleton<DatabaseInitializer>();
    builder.Services.AddSingleton<IAvailabilityRepository, AdoNetAvailabilityRepository>();
}
else
{
    builder.Services.AddSingleton<InMemoryAvailabilityStore>();
    builder.Services.AddSingleton<IAvailabilityRepository, InMemoryAvailabilityRepository>();
}

var issuer = builder.Configuration["Auth:LocalIssuer"] ?? throw new InvalidOperationException("Auth:LocalIssuer is required.");
var audience = builder.Configuration["Auth:LocalAudience"] ?? throw new InvalidOperationException("Auth:LocalAudience is required.");
var signingKey = builder.Configuration["Auth:LocalSigningKey"] ?? throw new InvalidOperationException("Auth:LocalSigningKey is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetService<DatabaseInitializer>();
    if (dbInitializer is not null)
    {
        await dbInitializer.InitializeAsync(CancellationToken.None);
    }
}

await AvailabilitySeedData.SeedAsync(app.Services, CancellationToken.None);

app.Run();

public partial class Program
{
}
