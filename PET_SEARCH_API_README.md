# Pet Search API Implementation

This implementation provides a complete pet search functionality that spans from the Matcher frontend through the Matcher backend to the ShelterHub backend, enabling location-based pet searches with filters for species and breed.

## Architecture Overview

The search functionality follows this flow:

1. **Matcher Frontend** → Calls PetSearchApi
2. **Matcher Backend** → PetSearchController receives request
3. **Matcher Backend** → PetSearchService validates and forwards to ShelterHub
4. **ShelterHub Backend** → PetsController (internal endpoint) performs the search
5. **ShelterHub Backend** → PetService executes PostGIS spatial queries
6. **Response flows back** through the same chain with pet results

## Components Implemented

### Backend Services

#### Matcher Service

- **Models**: `PetSearchRequest`, `PetSearchResponse`, `PetSearchResultDto`, `PetSearchShelterDto`
- **Controller**: `PetSearchController` with `/api/PetSearch/search` endpoint
- **Service**: `PetSearchService` for validation and external API calls
- **Authentication**: Requires authenticated user

#### ShelterHub Service

- **Models**: Matching search DTOs for consistency
- **Controller**: Internal `/api/internal/Pets/search` endpoint
- **Service**: `SearchPetsByLocationAsync` method with PostGIS spatial queries
- **Database**: PostgreSQL with PostGIS for distance calculations

### Frontend Implementation

#### Generated API Client

- **PetSearchApi**: Auto-generated TypeScript client
- **Models**: Type-safe request/response interfaces
- **Integration**: Added to Matcher app configuration

#### Browse Component Enhancement

- **Real API Integration**: Uses search API when location is provided
- **No Fallback Data**: Requires location for pet search functionality
- **Pagination**: Supports next token-based pagination
- **Empty State**: Shows helpful message when no location provided

#### Search Demo Component

- **Direct API Testing**: Simple interface for testing search functionality
- **Parameter Control**: Manual input for coordinates, distance, species
- **Results Display**: Shows search results with pet details
- **Load More**: Demonstrates pagination functionality

## Testing the Implementation

### 1. Search Demo Page

Navigate to `/search-demo` in the Matcher frontend to test the search API directly:

**Melbourne Demo:**

- Click "Load Melbourne Demo" to populate coordinates (-37.8136, 144.9631)
- Adjust max distance (default: 50km)
- Select species (optional)
- Click "Search Pets"

**Custom Search:**

- Enter latitude/longitude coordinates
- Set maximum distance in kilometers
- Optionally select a species
- Set page size (1-50 pets per page)
- Click "Search Pets"

### 2. Browse Page Integration

The existing browse page at `/browse` now requires location input to show pets:

- User must provide a location (uses Google Places autocomplete)
- Shows helpful empty state when no location provided
- Uses real search API when location coordinates are available

### 3. API Endpoints

#### Matcher Service

```http
POST https://localhost:7001/api/PetSearch/search
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "latitude": -37.8136,
  "longitude": 144.9631,
  "maxDistanceKm": 50,
  "speciesId": 1,
  "breedId": 5,
  "pageSize": 10,
  "nextToken": "optional-pagination-token"
}
```

#### ShelterHub Internal Endpoint

```http
GET https://localhost:7051/api/internal/Pets/search?latitude=-37.8136&longitude=144.9631&maxDistanceKm=50&speciesId=1&breedId=5&pageSize=10&nextToken=token
Authorization: InternalApiKey <internal-key>
```

## Search Parameters

| Parameter       | Type   | Required | Description                                       |
| --------------- | ------ | -------- | ------------------------------------------------- |
| `latitude`      | number | Yes      | Latitude coordinate for search center             |
| `longitude`     | number | Yes      | Longitude coordinate for search center            |
| `maxDistanceKm` | number | Yes      | Maximum distance from coordinates (km)            |
| `speciesId`     | number | No       | Filter by specific species (null=all species, 1=Dogs, 2=Cats, etc.) |
| `breedId`       | number | No       | Filter by specific breed (requires speciesId)     |
| `pageSize`      | number | No       | Number of results per page (default: 10, max: 50) |
| `nextToken`     | string | No       | Pagination token for next page                    |

## Response Format

```json
{
  "success": true,
  "pets": [
    {
      "petId": "pet-123",
      "name": "Buddy",
      "species": "Dog",
      "breed": "Golden Retriever",
      "ageInMonths": 36,
      "gender": "Male",
      "description": "Friendly and energetic...",
      "adoptionFee": 250,
      "mainImageFileExtension": "jpg",
      "distanceKm": 5.2,
      "shelter": {
        "shelterId": "shelter-456",
        "shelterName": "Melbourne Animal Rescue",
        "shelterAddress": "123 Pet Street, Melbourne VIC 3000",
        "shelterContactNumber": "+61 3 1234 5678",
        "shelterLatitude": -37.818,
        "shelterLongitude": 144.967
      }
    }
  ],
  "nextToken": "next-page-token",
  "totalCount": 45
}
```

## Development Notes

### Database Requirements

- PostgreSQL with PostGIS extension for spatial queries
- Shelter data with latitude/longitude coordinates
- Pet data with species and breed foreign keys

### Error Handling

- Location validation (valid coordinates)
- Distance limits (reasonable search radius)
- Species/breed validation against database
- Pagination token validation
- Database connection error handling

### Performance Considerations

- PostGIS spatial index on shelter coordinates
- Efficient distance calculations using ST_DWithin
- Pagination to limit result size
- DynamoDB query optimization for pet data

### Future Enhancements

- Add age range filters (min/max age)
- Add gender filter
- Add size/weight filters
- Add temperament/behavior filters
- Add photo availability filter
- Add shelter features filter
- Implement search result caching
- Add search analytics tracking

## Troubleshooting

### Common Issues

1. **No results found**: Check if coordinates are in area with shelters
2. **Authentication errors**: Ensure valid JWT token for Matcher API
3. **Internal API errors**: Verify internal API key configuration
4. **Database errors**: Check PostgreSQL connection and PostGIS extension

### Debug Tips

- Use search demo page for isolated testing
- Check browser network tab for API calls
- Monitor backend logs for error details
- Verify database content with direct queries

### Test Coordinates

- **Melbourne, Australia**: -37.8136, 144.9631
- **Sydney, Australia**: -33.8688, 151.2093
- **Brisbane, Australia**: -27.4698, 153.0251
