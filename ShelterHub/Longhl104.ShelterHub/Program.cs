using Microsoft.AspNetCore.Authorization;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Longhl104.PawfectMatch.Extensions;
using Longhl104.ShelterHub.Services;
using Longhl104.PawfectMatch.Middleware;
using System.Text.Json.Serialization;

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

// Register ShelterHub services
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

// Add authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

app.Run();
