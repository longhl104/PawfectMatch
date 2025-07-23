using Microsoft.AspNetCore.Authorization;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Longhl104.PawfectMatch.Extensions;
using Longhl104.ShelterHub.Services;
using Longhl104.PawfectMatch.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

// Add AWS Systems Manager configuration
builder.AddPawfectMatchSystemsManager("ShelterHub");

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddPawfectMatchInternalHttpClients();
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("shelter_admin");

// Configure AWS services
builder.Services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
builder.Services.AddSingleton<IAmazonS3, AmazonS3Client>();

// Configure Data Protection for containerized environment
builder.Services.AddPawfectMatchDataProtection("ShelterHub", environmentName);

// Configure caching
builder.Services.AddMemoryCache();

// Register ShelterHub services
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IMediaService, MediaService>();

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
                "https://localhost:4202",
                $"https://shelter.{environmentName.ToLowerInvariant()}.pawfectmatchnow.com",
                "https://shelter.pawfectmatchnow.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure forwarded headers for ALB (Application Load Balancer)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = null;
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure forwarded headers FIRST
app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Note: UseHttpsRedirection removed - ALB handles HTTPS termination

app.UseCors();

// Add authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint - allow anonymous access for ALB health checks
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();
