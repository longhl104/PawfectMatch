# Database Extensions

This extension provides PostgreSQL database configuration using AWS Secrets Manager for PawfectMatch applications.

## Usage

### Basic Usage

```csharp
using Longhl104.PawfectMatch.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL database using AWS Secrets Manager
await builder.Services.AddPawfectMatchPostgreSqlAsync<YourDbContext>(builder.Configuration);

var app = builder.Build();
```

### Custom Secret ARN Configuration Key

By default, the extension looks for the secret ARN in the `Database:SecretArn` configuration key. You can specify a different key:

```csharp
await builder.Services.AddPawfectMatchPostgreSqlAsync<YourDbContext>(
    builder.Configuration, 
    "MyCustom:SecretArn");
```

## Configuration Requirements

The extension requires a configuration value containing the ARN of an AWS Secrets Manager secret:

```json
{
  "Database": {
    "SecretArn": "arn:aws:secretsmanager:region:account:secret:your-secret-name"
  }
}
```

## Secret Format

The AWS Secrets Manager secret must contain JSON with the following camelCase properties:

```json
{
  "engine": "postgres",
  "host": "your-database-host",
  "username": "your-username",
  "password": "your-password", 
  "dbname": "your-database-name",
  "port": 5432
}
```

## Dependencies

This extension automatically:

- Registers `IAmazonSecretsManager` for dependency injection
- Configures Entity Framework Core with Npgsql PostgreSQL provider
- Handles secret retrieval and connection string generation

## Error Handling

The extension provides clear error messages for:

- Missing secret ARN configuration
- Failed secret retrieval from AWS Secrets Manager
- Invalid or malformed secret JSON

## Security

- Database credentials are securely stored in AWS Secrets Manager
- Supports automatic credential rotation
- No sensitive data stored in application configuration files

## Example Integration

```csharp
using Microsoft.EntityFrameworkCore;
using Longhl104.PawfectMatch.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add AWS Systems Manager configuration
builder.AddPawfectMatchSystemsManager("YourApp");

// Configure PostgreSQL database
await builder.Services.AddPawfectMatchPostgreSqlAsync<AppDbContext>(builder.Configuration);

// Your DbContext will be automatically configured and ready for dependency injection
var app = builder.Build();
```
