using Longhl104.PawfectMatch.Extensions;
using Amazon.DynamoDBv2;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine($"Environment: {environmentName}");

builder.AddPawfectMatchSystemsManager("Matcher");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
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

// Health check endpoint - allow anonymous access for ALB health checks
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .AllowAnonymous();

app.Run();
