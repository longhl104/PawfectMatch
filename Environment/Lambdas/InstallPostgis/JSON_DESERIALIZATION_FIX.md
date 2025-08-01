# PostGIS Lambda Function - JSON Deserialization Fix

## Issue Fixed

The Lambda function was failing with a Secrets Manager authentication error due to incorrect JSON property deserialization.

### Problem
The AWS Secrets Manager secret was stored with camelCase property names:
```json
{
  "password": ".q,Ik73k9pgh5pr2HiY,wTKq-ERdde",
  "dbname": "pawfectmatch", 
  "engine": "postgres",
  "port": 5432,
  "host": "pawfectmatch-development-postgresqldatabase03fc65-wyptb90wtlhq.cemp9ofgkqua.ap-southeast-2.rds.amazonaws.com",
  "username": "dbadmin"
}
```

But the `DatabaseSecret` class was expecting PascalCase properties, causing deserialization to fail and resulting in empty/null values.

### Solution

Updated the `DatabaseSecret` class to use `JsonPropertyName` attributes to correctly map the camelCase JSON properties:

```csharp
public class DatabaseSecret
{
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = string.Empty;
    
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("dbname")]
    public string DbName { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public int Port { get; set; }
}
```

### Changes Made

1. **Added JSON property mapping**: Added `[JsonPropertyName]` attributes to map camelCase JSON to PascalCase properties
2. **Added using statement**: Added `using System.Text.Json.Serialization;` for the attribute
3. **Verified fix**: Build and tests pass successfully

### Result

The Lambda function can now correctly deserialize the AWS Secrets Manager secret and connect to the PostgreSQL database to install PostGIS.

## Testing

✅ **Build Status**: Compiles successfully  
✅ **Unit Tests**: All 4 tests pass  
✅ **Ready for Deployment**: Lambda function should now work correctly with the custom resource

## Next Steps

Deploy the updated Lambda function and the custom resource should successfully invoke it to install PostGIS in the database.
