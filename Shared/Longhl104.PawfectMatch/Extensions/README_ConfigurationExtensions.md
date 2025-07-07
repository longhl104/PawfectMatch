# Configuration Extensions

This module provides configuration extensions for PawfectMatch services to standardize AWS Systems Manager Parameter Store integration.

## AddPawfectMatchSystemsManager Extension

The `AddPawfectMatchSystemsManager` extension method simplifies adding AWS Systems Manager Parameter Store configuration to PawfectMatch services.

### Usage

```csharp
using Longhl104.PawfectMatch.Extensions;

var builder = WebApplication.CreateBuilder(args);
var environmentName = builder.Environment.EnvironmentName;

// Option 1: Use standard PawfectMatch parameter path pattern
builder.Configuration.AddPawfectMatchSystemsManager(environmentName, "ServiceName");

// Option 2: Use custom parameter path
builder.Configuration.AddPawfectMatchSystemsManager("/Custom/Parameter/Path");
```

### Parameter Path Pattern

The extension follows the standard PawfectMatch parameter path pattern:

```text
/PawfectMatch/{EnvironmentName}/{ServiceName}
```

Where:

- `EnvironmentName`: Development, Staging, Production, etc.
- `ServiceName`: Identity, ShelterHub, Matcher, etc.

### Examples

```csharp
// For Identity service in Development environment
builder.Configuration.AddPawfectMatchSystemsManager("Development", "Identity");
// Resolves to: /PawfectMatch/Development/Identity

// For ShelterHub service in Production environment  
builder.Configuration.AddPawfectMatchSystemsManager("Production", "ShelterHub");
// Resolves to: /PawfectMatch/Production/ShelterHub

// For custom parameter path
builder.Configuration.AddPawfectMatchSystemsManager("/MyApp/Custom/Config");
```

### Requirements

- AWS credentials must be configured (via IAM role, AWS profile, or environment variables)
- The application must have appropriate permissions to read from AWS Systems Manager Parameter Store
- The `Amazon.Extensions.Configuration.SystemsManager` package is included as a dependency

### Migration from Direct AddSystemsManager

**Before:**

```csharp
builder.Configuration.AddSystemsManager($"/PawfectMatch/{environmentName}/Identity");
```

**After:**

```csharp
using Longhl104.PawfectMatch.Extensions;

builder.Configuration.AddPawfectMatchSystemsManager(environmentName, "Identity");
```

This provides better consistency across services and makes parameter path management more maintainable.
