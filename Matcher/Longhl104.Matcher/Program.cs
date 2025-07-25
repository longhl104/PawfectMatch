using Longhl104.PawfectMatch.Extensions;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

builder.AddPawfectMatchSystemsManager("Matcher");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Matcher API",
        Version = "v1",
        Description = "PawfectMatch Matcher Service API"
    });
});
builder.Services.AddPawfectMatchInternalHttpClients();

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

// Configure AWS services
builder.Services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();

// Configure Data Protection for containerized environment
builder.Services.AddPawfectMatchDataProtection("Matcher", environmentName);

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
    app.MapOpenApi().AllowAnonymous();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Matcher API V1");
        c.RoutePrefix = "swagger";
    });
    app.UseDeveloperExceptionPage();
}

// Note: UseHttpsRedirection removed - ALB handles HTTPS termination

app.UseCors();

// Add authentication middleware
app.UseMiddleware<Longhl104.PawfectMatch.Middleware.AuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint - allow anonymous access for ALB health checks
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();
