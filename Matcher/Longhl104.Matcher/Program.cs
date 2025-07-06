
using Longhl104.PawfectMatch.Middleware;
using Microsoft.AspNetCore.Authorization;
using Longhl104.PawfectMatch.Extensions;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add authentication and authorization services
// Configure custom authentication scheme to work with the AuthenticationMiddleware
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("adopter");

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["https://localhost:4200"]
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors();

// Add authentication middleware
app.UseMiddleware<Longhl104.PawfectMatch.Middleware.AuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

app.Run();
