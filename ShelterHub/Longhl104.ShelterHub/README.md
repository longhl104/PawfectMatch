# ShelterHub REST API

This document describes the REST API endpoints for creating and managing shelter admin profiles and their associated shelters in the PawfectMatch ShelterHub service.

## Overview

The ShelterHub API provides endpoints for:
- Creating shelter admin profiles and associated shelters (called by Identity service during registration)
- Retrieving shelter admin profiles
- Managing shelter information

## API Endpoints

### 1. Create Shelter Admin (Internal)

**Endpoint:** `POST /api/shelter-admins`  
**Authorization:** Internal API Key Required  
**Purpose:** Creates a shelter admin profile and associated shelter (called by Identity service during registration)

**Request Body:**
```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "shelterName": "Happy Paws Animal Shelter",
  "shelterContactNumber": "0412345678",
  "shelterAddress": "123 Pet Street, Sydney NSW 2000",
  "shelterWebsiteUrl": "https://happypaws.com.au",
  "shelterAbn": "12345678901",
  "shelterDescription": "A loving shelter dedicated to finding homes for abandoned pets."
}
```

**Response:**
```json
{
  "success": true,
  "message": "Shelter admin profile created successfully",
  "userId": "12345678-1234-1234-1234-123456789012",
  "shelterId": "abcdef12-3456-7890-abcd-ef1234567890"
}
```

### 2. Get Current Shelter Admin Profile

**Endpoint:** `GET /api/shelter-admins/profile`  
**Authorization:** Authenticated shelter admin required  
**Purpose:** Retrieves the current authenticated shelter admin's profile

**Response:**
```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "shelterId": "abcdef12-3456-7890-abcd-ef1234567890",
  "createdAt": "2023-01-01T00:00:00Z",
  "updatedAt": "2023-01-01T00:00:00Z"
}
```

### 3. Get My Shelter

**Endpoint:** `GET /api/shelters/my-shelter`  
**Authorization:** Authenticated shelter admin required  
**Purpose:** Retrieves the shelter information for the current authenticated shelter admin

**Response:**
```json
{
  "shelterId": "abcdef12-3456-7890-abcd-ef1234567890",
  "shelterName": "Happy Paws Animal Shelter",
  "shelterContactNumber": "0412345678",
  "shelterAddress": "123 Pet Street, Sydney NSW 2000",
  "shelterWebsiteUrl": "https://happypaws.com.au",
  "shelterAbn": "12345678901",
  "shelterDescription": "A loving shelter dedicated to finding homes for abandoned pets.",
  "isActive": true,
  "createdAt": "2023-01-01T00:00:00Z",
  "updatedAt": "2023-01-01T00:00:00Z"
}
```

### 4. Get Shelter Admin by ID (Internal)

**Endpoint:** `GET /api/shelter-admins/{userId}`  
**Authorization:** Internal API Key Required  
**Purpose:** Retrieves a shelter admin profile by user ID (for internal service communication)

### 5. Get Shelter by ID (Internal)

**Endpoint:** `GET /api/shelters/{shelterId}`  
**Authorization:** Internal API Key Required  
**Purpose:** Retrieves shelter information by shelter ID (for internal service communication)

### 6. Get My Shelter Pet Statistics

**Endpoint:** `GET /api/shelters/my-shelter/pet-statistics`  
**Authorization:** Authenticated shelter admin required  
**Purpose:** Retrieves pet statistics for the current authenticated shelter admin's shelter

**Response:**
```json
{
  "success": true,
  "totalPets": 150,
  "adoptedPets": 120,
  "availablePets": 25,
  "pendingPets": 3,
  "medicalHoldPets": 2,
  "fromCache": true,
  "lastUpdated": "2025-07-13T10:30:00Z"
}
```

### 7. Get Shelter Pet Statistics by ID

**Endpoint:** `GET /api/shelters/{shelterId}/pet-statistics`  
**Authorization:** Authenticated shelter admin required  
**Purpose:** Retrieves pet statistics for a specific shelter by shelter ID

**Response:**
```json
{
  "success": true,
  "totalPets": 150,
  "adoptedPets": 120,
  "availablePets": 25,
  "pendingPets": 3,
  "medicalHoldPets": 2,
  "fromCache": false,
  "lastUpdated": "2025-07-13T10:30:00Z",
  "errorMessage": null
}
```

## Authentication

### For Shelter Admins
- Endpoints requiring shelter admin authentication use JWT tokens
- Tokens are passed via cookies or Authorization header
- Only users with `shelter_admin` user type can access these endpoints

### For Internal Services
- Internal endpoints require the `X-Internal-API-Key` header
- This is used for service-to-service communication
- The Identity service uses this to create shelter admin profiles during registration

## Caching

### Pet Statistics Caching
- Pet statistics are cached in memory for 10 minutes with a 5-minute sliding expiration
- Cache is automatically invalidated when pets are created, updated, or deleted
- The `fromCache` field in the response indicates if data was retrieved from cache
- Cache keys are based on shelter ID: `shelter-pet-stats:{shelterId}`

### Pet Count Caching  
- Pet counts (used for pagination) are cached in memory for 5 minutes with a 2-minute sliding expiration
- Cache includes filter-specific counts (status, species, name, breed)
- Cache is automatically invalidated when pets are created, updated, or deleted
- Cache keys include all filter parameters for accurate results

## Data Models

### CreateShelterAdminRequest
```csharp
public class CreateShelterAdminRequest
{
    public string UserId { get; set; }              // Required
    public string ShelterName { get; set; }         // Required
    public string ShelterContactNumber { get; set; } // Required
    public string ShelterAddress { get; set; }      // Required
    public string? ShelterWebsiteUrl { get; set; }  // Optional
    public string? ShelterAbn { get; set; }         // Optional
    public string? ShelterDescription { get; set; } // Optional
}
```

### ShelterAdmin
```csharp
public class ShelterAdmin
{
    public string UserId { get; set; }        // Cognito User ID (Primary Key)
    public string ShelterId { get; set; }     // Associated Shelter ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Shelter
```csharp
public class Shelter
{
    public string ShelterId { get; set; }             // Primary Key
    public string ShelterName { get; set; }
    public string ShelterContactNumber { get; set; }
    public string ShelterAddress { get; set; }
    public string? ShelterWebsiteUrl { get; set; }
    public string? ShelterAbn { get; set; }
    public string? ShelterDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Database Schema

### DynamoDB Tables

**Table: `pawfectmatch-{environment}-shelter-hub-shelter-admins`**
- Primary Key: `UserId` (String)
- Attributes: `ShelterId`, `CreatedAt`, `UpdatedAt`

**Table: `pawfectmatch-{environment}-shelter-hub-shelters`**
- Primary Key: `ShelterId` (String)
- Attributes: `ShelterName`, `ShelterContactNumber`, `ShelterAddress`, `ShelterWebsiteUrl`, `ShelterAbn`, `ShelterDescription`, `IsActive`, `CreatedAt`, `UpdatedAt`

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error responses include a message explaining the issue:
```json
{
  "success": false,
  "message": "Error description",
  "userId": "user-id-if-applicable"
}
```

## Testing

Use the provided `ShelterHub-API-Tests.http` file to test the API endpoints. Update the URLs and authentication tokens as needed for your environment.

## Integration with Identity Service

The Identity service automatically calls the `POST /api/shelter-admins` endpoint during shelter admin registration to create the shelter admin profile and associated shelter. This ensures that when a shelter admin registers, they immediately have both a user account and a shelter profile created.

## Running the Service

1. Ensure DynamoDB tables are created (via CDK deployment)
2. Configure AWS credentials and region
3. Set up Parameter Store configuration for ShelterHub
4. Run the service: `dotnet run`
5. The service will be available at `https://localhost:7002`

## Dependencies

- AWS DynamoDB for data storage
- AWS Systems Manager Parameter Store for configuration
- Shared PawfectMatch library for common functionality
- JWT authentication via shared middleware
