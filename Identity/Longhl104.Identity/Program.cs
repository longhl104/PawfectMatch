using Amazon.DynamoDBv2;
using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Longhl104.Identity.Services;
using Longhl104.PawfectMatch.Extensions;
using Longhl104.PawfectMatch.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

builder.AddPawfectMatchSystemsManager("Identity");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddPawfectMatchInternalHttpClients(
    [
        PawfectMatchServices.Matcher,
    ]);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                [
                    "https://localhost:4200",
                    $"https://id.{environmentName.ToLowerInvariant()}.pawfectmatchnow.com",
                    "https://id.pawfectmatchnow.com"
                ]
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure AWS services
builder.Services.AddSingleton<AmazonDynamoDBClient>();
builder.Services.AddSingleton<AmazonCognitoIdentityProviderClient>();

// Configure Data Protection for containerized environment
builder.Services.AddPawfectMatchDataProtection("Identity", environmentName);

// Configure Identity services
builder.Services.AddScoped<ICognitoService, CognitoService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Configure JWT authentication
var jwtKey = builder.Configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key configuration is required");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "PawfectMatch";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "PawfectMatch";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow tokens from cookies
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies["accessToken"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure forwarded headers for ALB (Application Load Balancer)
// ALB terminates HTTPS and forwards HTTP to containers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Trust the ALB/proxy headers
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // Accept forwarded headers from any source (ALB)
    options.ForwardLimit = null;
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure forwarded headers FIRST (before other middleware)
app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

// Note: UseHttpsRedirection is NOT needed here because:
// 1. ALB handles HTTPS termination and HTTP->HTTPS redirects
// 2. Container only receives HTTP traffic from ALB
// 3. Using it causes "Failed to determine the https port for redirect" error

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint - allow anonymous access for ALB health checks
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .AllowAnonymous();

app.Run();
