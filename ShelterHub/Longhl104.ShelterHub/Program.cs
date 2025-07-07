using Longhl104.PawfectMatch.Middleware;
using Microsoft.AspNetCore.Authorization;
using Amazon.DynamoDBv2;
using Longhl104.PawfectMatch.Extensions;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

// Add AWS Systems Manager configuration
builder.AddPawfectMatchSystemsManager("ShelterHub");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure AWS services
builder.Services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
builder.Services.AddPawfectMatchInternalHttpClients();

// Register ShelterHub services
builder.Services.AddScoped<Longhl104.ShelterHub.Services.IShelterService, Longhl104.ShelterHub.Services.ShelterService>();

// Add authentication and authorization services
// Note: AuthenticationMiddleware handles JWT validation and sets ClaimsPrincipal
builder.Services.AddAuthentication();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("UserType", "shelter_admin")
        .Build())
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("UserType", "shelter_admin")
        .Build())
    .AddPolicy("ShelterAdminOnly", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("UserType", "shelter_admin"));

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
