using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Longhl104.ShelterHub.Models;
using Longhl104.PawfectMatch.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PostgreSqlPet = Longhl104.ShelterHub.Models.PostgreSql.Pet;

namespace Longhl104.ShelterHub.Services;

/// <summary>
/// Interface for pet data service
/// </summary>
public interface IPetService
{
    /// <summary>
    /// Gets all pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>List of pets</returns>
    Task<GetPetsResponse> GetPetsByShelterId(Guid shelterId);

    /// <summary>
    /// Gets paginated pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="request">Pagination request parameters</param>
    /// <returns>Paginated list of pets</returns>
    Task<GetPaginatedPetsResponse> GetPaginatedPetsByShelterId(Guid shelterId, GetPetsRequest request);

    /// <summary>
    /// Gets a specific pet by ID
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet details</returns>
    Task<PetResponse> GetPetById(Guid petId);

    /// <summary>
    /// Creates a new pet
    /// </summary>
    /// <param name="request">Pet creation request</param>
    /// <param name="shelterId">The shelter ID that owns the pet</param>
    /// <returns>Created pet</returns>
    Task<PetResponse> CreatePet(CreatePetRequest request, Guid shelterId);

    /// <summary>
    /// Updates an existing pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="request">Pet update request</param>
    /// <returns>Updated pet</returns>
    Task<PetResponse> UpdatePet(Guid petId, UpdatePetRequest request);

    /// <summary>
    /// Updates a pet's status
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated pet</returns>
    Task<PetResponse> UpdatePetStatus(Guid petId, PetStatus status);

    /// <summary>
    /// Deletes a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Success status</returns>
    Task<PetResponse> DeletePet(Guid petId);

    /// <summary>
    /// Generates a presigned URL for uploading pet images
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="fileName">The name of the file to upload</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="fileSizeBytes">The size of the file in bytes</param>
    /// <returns>Presigned URL for upload</returns>
    Task<PresignedUrlResponse> GenerateUploadUrl(Guid petId, string fileName, string contentType, long fileSizeBytes);

    /// <summary>
    /// Gets download presigned URLs for main images of multiple pets
    /// </summary>
    /// <param name="petIds">List of pet IDs to get image URLs for</param>
    /// <returns>Dictionary of pet IDs to their download presigned URLs</returns>
    Task<PetImageDownloadUrlsResponse> GetPetImageDownloadUrls(List<Guid> petIds);

    /// <summary>
    /// Gets download presigned URLs for main images of multiple pets with their file extensions
    /// </summary>
    /// <param name="request">Request containing pet IDs and their file extensions</param>
    /// <returns>Dictionary of pet IDs to their download presigned URLs</returns>
    Task<PetImageDownloadUrlsResponse> GetPetImageDownloadUrls(GetPetImageDownloadUrlsRequest request);

    /// <summary>
    /// Gets pet statistics for a specific shelter with caching
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>Pet statistics including total and adopted counts</returns>
    Task<ShelterPetStatisticsResponse> GetShelterPetStatistics(Guid shelterId);

    /// <summary>
    /// Gets all media files (images, videos, documents) for a specific pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet media files organized by type</returns>
    Task<GetPetMediaResponse> GetPetMedia(Guid petId);

    /// <summary>
    /// Generates presigned URLs for uploading multiple media files for a pet
    /// </summary>
    /// <param name="request">Request containing media file details</param>
    /// <returns>Presigned URLs for uploading media files</returns>
    Task<MediaFileUploadResponse> GenerateMediaUploadUrls(UploadMediaFilesRequest request);

    /// <summary>
    /// Confirms successful upload of media files and updates the database
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="mediaFileIds">List of media file IDs that were successfully uploaded</param>
    /// <returns>Updated pet media response</returns>
    Task<GetPetMediaResponse> ConfirmMediaUploads(Guid petId, List<Guid> mediaFileIds);

    /// <summary>
    /// Deletes multiple media files for a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="request">Request containing media file IDs to delete</param>
    /// <returns>Delete operation results</returns>
    Task<DeleteMediaFilesResponse> DeleteMediaFiles(Guid petId, DeleteMediaFilesRequest request);

    /// <summary>
    /// Updates the display order of media files for a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="mediaFileOrders">Dictionary of media file ID to display order</param>
    /// <returns>Updated pet media response</returns>
    Task<GetPetMediaResponse> ReorderMediaFiles(Guid petId, Dictionary<Guid, int> mediaFileOrders);

    /// <summary>
    /// Gets all available pet species
    /// </summary>
    /// <returns>List of all pet species</returns>
    Task<GetPetSpeciesResponse> GetAllPetSpecies();

    /// <summary>
    /// Gets all breeds for a specific species
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>List of breeds for the species</returns>
    Task<GetPetBreedsResponse> GetBreedsBySpeciesId(int speciesId);
}

/// <summary>
/// Service for managing pet operations with DynamoDB
/// </summary>
public class PetService : IPetService
{
    private readonly AppDbContext _dbContext;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PetService> _logger;
    private readonly IMediaService _mediaUploadService;
    private readonly IMemoryCache _memoryCache;
    private readonly IShelterService _shelterService;
    private readonly string _tableName;
    private readonly string _bucketName;
    private readonly string _shelterIdCreatedAtIndex = "ShelterIdCreatedAtIndex";
    private readonly string _petMediaFilesTableName;

    public PetService(
        AppDbContext dbContext,
        IAmazonDynamoDB dynamoDbClient,
        IHostEnvironment environment,
        ILogger<PetService> logger,
        IMediaService mediaUploadService,
        IMemoryCache memoryCache,
        IShelterService shelterService
        )
    {
        _dbContext = dbContext;
        _dynamoDbClient = dynamoDbClient;
        _environment = environment;
        _logger = logger;
        _mediaUploadService = mediaUploadService;
        _memoryCache = memoryCache;
        _shelterService = shelterService;
        _tableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pets";
        _bucketName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pet-media";
        _petMediaFilesTableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pet-media-files";
    }

    /// <summary>
    /// Gets all pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>List of pets</returns>
    public async Task<GetPetsResponse> GetPetsByShelterId(Guid shelterId)
    {
        try
        {
            _logger.LogInformation("Getting pets for shelter ID: {ShelterId}", shelterId);

            var query = new QueryRequest
            {
                TableName = _tableName,
                IndexName = _shelterIdCreatedAtIndex,
                KeyConditionExpression = "ShelterId = :shelterId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":shelterId", new AttributeValue { S = shelterId.ToString() } }
                },
                ScanIndexForward = false // Get most recent pets first
            };

            var response = await _dynamoDbClient.QueryAsync(query);
            var pets = response.Items.Select(ConvertDynamoDbItemToPet).ToList();
            var petPostgreSqlIds = pets.Select(p => p.PetPostgreSqlId);
            var postgresPets = await _dbContext.Pets
                .Where(p => petPostgreSqlIds.Contains(p.PetId))
                .ToListAsync();

            // Enrich pets with PostgreSQL data
            foreach (var pet in pets)
            {
                var postgresPet = postgresPets.FirstOrDefault(p => p.PetId == pet.PetPostgreSqlId);
                if (postgresPet != null)
                {
                    pet.Name = postgresPet.Name;
                    pet.DateOfBirth = postgresPet.DateOfBirth;
                    pet.Gender = postgresPet.Gender;
                    pet.AdoptionFee = postgresPet.AdoptionFee;
                    pet.Description = postgresPet.Description;
                    pet.Status = postgresPet.Status;
                    pet.CreatedAt = postgresPet.CreatedAt;
                    pet.MainImageFileExtension = postgresPet.MainImageFileExtension;
                }
            }

            _logger.LogInformation("Found {PetCount} pets for shelter {ShelterId}", pets.Count, shelterId);

            return new GetPetsResponse
            {
                Success = true,
                Pets = pets
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pets for shelter {ShelterId}", shelterId);
            return new GetPetsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve pets: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets paginated pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="request">Pagination request parameters</param>
    /// <returns>Paginated list of pets</returns>
    public async Task<GetPaginatedPetsResponse> GetPaginatedPetsByShelterId(Guid shelterId, GetPetsRequest request)
    {
        try
        {
            _logger.LogInformation("Getting paginated pets for shelter ID: {ShelterId}, PageSize: {PageSize}, Status: {Status}, Species: {Species}, Name: {Name}, Breed: {Breed}",
                shelterId, request.PageSize, request.Status, request.Species, request.Name, request.Breed);

            // Validate page size
            var pageSize = Math.Min(Math.Max(request.PageSize, 1), 100);

            var query = new QueryRequest
            {
                TableName = _tableName,
                IndexName = _shelterIdCreatedAtIndex, // GSI for querying by shelter ID
                KeyConditionExpression = "ShelterId = :shelterId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":shelterId", new AttributeValue { S = shelterId.ToString() } }
                },
                Limit = pageSize
            };

            // Build filter expression if filters are provided
            var filterConditions = new List<string>();

            if (request.Status.HasValue)
            {
                filterConditions.Add("#status = :status");
                query.ExpressionAttributeValues[":status"] = new AttributeValue { S = request.Status.Value.GetAmbientValue<string>() };
            }

            if (!string.IsNullOrWhiteSpace(request.Species))
            {
                filterConditions.Add("#species = :species");
                query.ExpressionAttributeValues[":species"] = new AttributeValue { S = request.Species };
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                filterConditions.Add("contains(#petName, :petName)");
                query.ExpressionAttributeValues[":petName"] = new AttributeValue { S = request.Name.ToLowerInvariant() };
            }

            if (!string.IsNullOrWhiteSpace(request.Breed))
            {
                filterConditions.Add("contains(#breed, :breed)");
                query.ExpressionAttributeValues[":breed"] = new AttributeValue { S = request.Breed.ToLowerInvariant() };
            }

            if (filterConditions.Count > 0)
            {
                query.FilterExpression = string.Join(" AND ", filterConditions);
                query.ExpressionAttributeNames = [];

                if (request.Status.HasValue)
                {
                    query.ExpressionAttributeNames["#status"] = "Status";
                }

                if (!string.IsNullOrWhiteSpace(request.Species))
                {
                    query.ExpressionAttributeNames["#species"] = "Species";
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    query.ExpressionAttributeNames["#petName"] = "Name";
                }

                if (!string.IsNullOrWhiteSpace(request.Breed))
                {
                    query.ExpressionAttributeNames["#breed"] = "Breed";
                }
            }

            // Add pagination token if provided
            if (!string.IsNullOrEmpty(request.NextToken))
            {
                try
                {
                    var lastKeyBytes = Convert.FromBase64String(request.NextToken);
                    var lastKeyJson = System.Text.Encoding.UTF8.GetString(lastKeyBytes);
                    var lastKey = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(lastKeyJson);
                    query.ExclusiveStartKey = lastKey;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid pagination token provided");
                    return new GetPaginatedPetsResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid pagination token"
                    };
                }
            }

            var response = await _dynamoDbClient.QueryAsync(query);
            var pets = response.Items.Select(ConvertDynamoDbItemToPet).ToList();

            // Enrich pets with PostgreSQL data
            if (pets.Count > 0)
            {
                var petPostgreSqlIds = pets.Select(p => p.PetPostgreSqlId);
                var postgresPets = await _dbContext.Pets
                    .Where(p => petPostgreSqlIds.Contains(p.PetId))
                    .ToListAsync();

                foreach (var pet in pets)
                {
                    var postgresPet = postgresPets.FirstOrDefault(p => p.PetId == pet.PetPostgreSqlId);
                    if (postgresPet != null)
                    {
                        pet.Name = postgresPet.Name;
                        pet.DateOfBirth = postgresPet.DateOfBirth;
                        pet.Gender = postgresPet.Gender;
                        pet.AdoptionFee = postgresPet.AdoptionFee;
                        pet.Description = postgresPet.Description;
                        pet.Status = postgresPet.Status;
                        pet.CreatedAt = postgresPet.CreatedAt;
                        pet.MainImageFileExtension = postgresPet.MainImageFileExtension;
                    }
                }
            }

            // Get total count with caching (including filters)
            var totalCount = await GetTotalPetCountWithCache(shelterId, request);

            // Generate next token if there are more items
            string? nextToken = null;
            if (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0)
            {
                var lastKeyJson = System.Text.Json.JsonSerializer.Serialize(response.LastEvaluatedKey);
                var lastKeyBytes = System.Text.Encoding.UTF8.GetBytes(lastKeyJson);
                nextToken = Convert.ToBase64String(lastKeyBytes);
            }

            _logger.LogInformation("Found {PetCount} pets for shelter {ShelterId} (Total: {TotalCount}) with filters", pets.Count, shelterId, totalCount);

            return new GetPaginatedPetsResponse
            {
                Success = true,
                Pets = pets,
                NextToken = nextToken,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paginated pets for shelter {ShelterId}", shelterId);
            return new GetPaginatedPetsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve pets: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets a specific pet by ID
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet details</returns>
    public async Task<PetResponse> GetPetById(Guid petId)
    {
        try
        {
            _logger.LogInformation("Getting pet by ID: {PetId}", petId);

            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                }
            };

            var response = await _dynamoDbClient.GetItemAsync(request);

            if (!response.IsItemSet)
            {
                _logger.LogWarning("Pet not found: {PetId}", petId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            var pet = ConvertDynamoDbItemToPet(response.Item);

            // Enrich pet with PostgreSQL data
            var postgresPet = await _dbContext.Pets.FindAsync(pet.PetPostgreSqlId);
            if (postgresPet != null)
            {
                pet.Name = postgresPet.Name;
                pet.DateOfBirth = postgresPet.DateOfBirth;
                pet.Gender = postgresPet.Gender;
                pet.AdoptionFee = postgresPet.AdoptionFee;
                pet.Description = postgresPet.Description;
                pet.Status = postgresPet.Status;
                pet.CreatedAt = postgresPet.CreatedAt;
                pet.MainImageFileExtension = postgresPet.MainImageFileExtension;
            }

            _logger.LogInformation("Successfully retrieved pet: {PetId}", petId);

            return new PetResponse
            {
                Success = true,
                Pet = pet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pet by ID: {PetId}", petId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve pet: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Creates a new pet
    /// </summary>
    /// <param name="request">Pet creation request</param>
    /// <param name="shelterId">The shelter ID that owns the pet</param>
    /// <returns>Created pet</returns>
    public async Task<PetResponse> CreatePet(CreatePetRequest request, Guid shelterId)
    {
        try
        {
            _logger.LogInformation("Creating new pet for shelter {ShelterId}", shelterId);

            // Get the shelter information to access the PostgreSQL shelter ID
            var shelter = await _shelterService.GetShelterAsync(shelterId);
            if (shelter == null)
            {
                _logger.LogWarning("Shelter not found: {ShelterId}", shelterId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Shelter not found"
                };
            }

            // First, create the pet in PostgreSQL
            var postgresPet = new PostgreSqlPet
            {
                Name = request.Name,
                SpeciesId = request.SpeciesId,
                BreedId = request.BreedId,
                ShelterId = shelter.ShelterPostgreSqlId,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Description = request.Description,
                AdoptionFee = request.AdoptionFee,
                IsSpayedNeutered = request.IsSpayedNeutered,
                IsVaccinated = request.IsVaccinated,
                IsMicrochipped = request.IsMicrochipped,
                IsGoodWithKids = request.IsGoodWithKids,
                IsGoodWithPets = request.IsGoodWithPets,
                IsHouseTrained = request.IsHouseTrained,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Pets.Add(postgresPet);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created PostgreSQL pet with ID: {PostgresPetId}", postgresPet.PetId);

            // Now create the API Pet model for DynamoDB
            var apiPet = new Pet
            {
                PetId = Guid.NewGuid(),
                PetPostgreSqlId = postgresPet.PetId,
                Weight = request.Weight,
                Color = request.Color,
                SpecialNeeds = request.SpecialNeeds,
                CreatedAt = DateTime.UtcNow,
                ShelterId = shelterId,
            };

            var putRequest = new PutItemRequest
            {
                TableName = _tableName,
                Item = ConvertPetToDynamoDbItem(apiPet)
            };

            await _dynamoDbClient.PutItemAsync(putRequest);

            // Invalidate cache for this shelter
            InvalidatePetCountCache(shelterId);
            InvalidatePetStatisticsCache(shelterId);

            _logger.LogInformation("Successfully created pet with API ID: {PetId} and PostgreSQL ID: {PostgresPetId}", apiPet.PetId, postgresPet.PetId);

            return new PetResponse
            {
                Success = true,
                Pet = apiPet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pet for shelter {ShelterId}", shelterId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to create pet: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates an existing pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="request">Pet update request</param>
    /// <returns>Updated pet</returns>
    public async Task<PetResponse> UpdatePet(Guid petId, UpdatePetRequest request)
    {
        try
        {
            _logger.LogInformation("Updating pet with ID: {PetId}", petId);

            // Look up species and breed names from their IDs
            var species = await _dbContext.PetSpecies.FindAsync(request.SpeciesId);
            var breed = await _dbContext.PetBreeds.FindAsync(request.BreedId);

            if (species == null)
            {
                _logger.LogWarning("Species not found with ID: {SpeciesId}", request.SpeciesId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Species not found"
                };
            }

            if (breed == null)
            {
                _logger.LogWarning("Breed not found with ID: {BreedId}", request.BreedId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Breed not found"
                };
            }

            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                },
                UpdateExpression = "SET #name = :name, #species = :species, #breed = :breed, #dateOfBirth = :dateOfBirth, #gender = :gender, #description = :description, #adoptionFee = :adoptionFee, #color = :color, #isSpayedNeutered = :isSpayedNeutered, #isVaccinated = :isVaccinated, #isMicrochipped = :isMicrochipped, #isHouseTrained = :isHouseTrained, #isGoodWithKids = :isGoodWithKids, #isGoodWithPets = :isGoodWithPets, #specialNeeds = :specialNeeds, #status = :status" + (request.Weight.HasValue ? ", #weight = :weight" : ""),
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#name", "Name" },
                    { "#species", "Species" },
                    { "#breed", "Breed" },
                    { "#dateOfBirth", "DateOfBirth" },
                    { "#gender", "Gender" },
                    { "#description", "Description" },
                    { "#adoptionFee", "AdoptionFee" },
                    { "#color", "Color" },
                    { "#isSpayedNeutered", "IsSpayedNeutered" },
                    { "#isVaccinated", "IsVaccinated" },
                    { "#isMicrochipped", "IsMicrochipped" },
                    { "#isHouseTrained", "IsHouseTrained" },
                    { "#isGoodWithKids", "IsGoodWithKids" },
                    { "#isGoodWithPets", "IsGoodWithPets" },
                    { "#specialNeeds", "SpecialNeeds" },
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":name", new AttributeValue { S = request.Name.ToLowerInvariant() } },
                    { ":species", new AttributeValue { S = species.Name } },
                    { ":breed", new AttributeValue { S = breed.Name.ToLowerInvariant() } },
                    { ":dateOfBirth", new AttributeValue { S = request.DateOfBirth.ToString("yyyy-MM-dd") } },
                    { ":gender", new AttributeValue { S = request.Gender } },
                    { ":description", new AttributeValue { S = request.Description } },
                    { ":adoptionFee", new AttributeValue { N = request.AdoptionFee.ToString("F2") } },
                    { ":color", new AttributeValue { S = request.Color } },
                    { ":isSpayedNeutered", new AttributeValue { BOOL = request.IsSpayedNeutered } },
                    { ":isVaccinated", new AttributeValue { BOOL = request.IsVaccinated } },
                    { ":isMicrochipped", new AttributeValue { BOOL = request.IsMicrochipped } },
                    { ":isHouseTrained", new AttributeValue { BOOL = request.IsHouseTrained } },
                    { ":isGoodWithKids", new AttributeValue { BOOL = request.IsGoodWithKids } },
                    { ":isGoodWithPets", new AttributeValue { BOOL = request.IsGoodWithPets } },
                    { ":specialNeeds", new AttributeValue { S = request.SpecialNeeds } },
                    { ":status", new AttributeValue { S = request.Status.ToString() } }
                },
                ConditionExpression = "attribute_exists(PetId)", // Ensure the pet exists
                ReturnValues = ReturnValue.ALL_NEW
            };

            // Add weight if provided
            if (request.Weight.HasValue)
            {
                updateRequest.ExpressionAttributeNames.Add("#weight", "Weight");
                updateRequest.ExpressionAttributeValues.Add(":weight", new AttributeValue { N = request.Weight.Value.ToString("F2") });
            }

            var response = await _dynamoDbClient.UpdateItemAsync(updateRequest);

            if (response.Attributes.Count == 0)
            {
                _logger.LogWarning("Pet not found for update: {PetId}", petId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            var updatedPet = ConvertDynamoDbItemToPet(response.Attributes);

            // Invalidate cache for this shelter since pet data changed
            var shelterId = await GetShelterIdForPet(updatedPet);
            if (shelterId.HasValue)
            {
                InvalidatePetCountCache(shelterId.Value);
                InvalidatePetStatisticsCache(shelterId.Value);
            }

            _logger.LogInformation("Successfully updated pet with ID: {PetId}", petId);

            return new PetResponse
            {
                Success = true,
                Pet = updatedPet
            };
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Pet not found when updating: {PetId}", petId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = "Pet not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update pet with ID: {PetId}", petId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to update pet: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates a pet's status
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated pet</returns>
    public async Task<PetResponse> UpdatePetStatus(Guid petId, PetStatus status)
    {
        try
        {
            _logger.LogInformation("Updating pet status for ID: {PetId} to {Status}", petId, status);

            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                },
                UpdateExpression = "SET #status = :status",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":status", new AttributeValue { S = status.GetAmbientValue<string>() } }
                },
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _dynamoDbClient.UpdateItemAsync(updateRequest);

            if (response.Attributes.Count == 0)
            {
                _logger.LogWarning("Pet not found for status update: {PetId}", petId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            var updatedPet = ConvertDynamoDbItemToPet(response.Attributes);

            // Invalidate cache for this shelter since pet status changed
            var shelterId = await GetShelterIdForPet(updatedPet);
            if (shelterId.HasValue)
            {
                InvalidatePetCountCache(shelterId.Value);
                InvalidatePetStatisticsCache(shelterId.Value);
            }

            _logger.LogInformation("Successfully updated pet status for ID: {PetId}", petId);

            return new PetResponse
            {
                Success = true,
                Pet = updatedPet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update pet status for ID: {PetId}", petId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to update pet status: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Deletes a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Success status</returns>
    public async Task<PetResponse> DeletePet(Guid petId)
    {
        try
        {
            _logger.LogInformation("Deleting pet with ID: {PetId}", petId);

            var deleteRequest = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                },
                ReturnValues = ReturnValue.ALL_OLD
            };

            var response = await _dynamoDbClient.DeleteItemAsync(deleteRequest);

            if (response.Attributes.Count == 0)
            {
                _logger.LogWarning("Pet not found for deletion: {PetId}", petId);
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            var deletedPet = ConvertDynamoDbItemToPet(response.Attributes);

            // Invalidate cache for this shelter since pet was deleted
            var shelterId = await GetShelterIdForPet(deletedPet);
            if (shelterId.HasValue)
            {
                InvalidatePetCountCache(shelterId.Value);
                InvalidatePetStatisticsCache(shelterId.Value);
            }

            _logger.LogInformation("Successfully deleted pet with ID: {PetId}", petId);

            return new PetResponse
            {
                Success = true,
                Pet = deletedPet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete pet with ID: {PetId}", petId);
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to delete pet: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the shelter ID for a pet by looking up the PostgreSQL pet record
    /// </summary>
    /// <param name="pet">The API pet object</param>
    /// <returns>The shelter ID as a Guid, or null if not found</returns>
    private async Task<Guid?> GetShelterIdForPet(Pet pet)
    {
        try
        {
            var postgresPet = await _dbContext.Pets.FindAsync(pet.PetPostgreSqlId);
            if (postgresPet != null)
            {
                // Look up the shelter by PostgreSQL ID to get the DynamoDB Guid
                return await GetShelterIdByPostgreSqlId(postgresPet.ShelterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get shelter ID for pet {PetId}", pet.PetId);
        }
        return null;
    }

    /// <summary>
    /// Gets the DynamoDB shelter ID (Guid) by PostgreSQL shelter ID
    /// </summary>
    /// <param name="postgresqlShelterId">The PostgreSQL shelter ID</param>
    /// <returns>The DynamoDB shelter ID as a Guid, or null if not found</returns>
    private async Task<Guid?> GetShelterIdByPostgreSqlId(int postgresqlShelterId)
    {
        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-shelters",
                FilterExpression = "ShelterPostgreSqlId = :postgresqlId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":postgresqlId"] = new AttributeValue { N = postgresqlShelterId.ToString() }
                },
                ProjectionExpression = "ShelterId"
            };

            var response = await _dynamoDbClient.ScanAsync(scanRequest);

            var matchingItem = response.Items.FirstOrDefault();
            if (matchingItem != null && matchingItem.TryGetValue("ShelterId", out var shelterIdAttr))
            {
                return Guid.Parse(shelterIdAttr.S);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get DynamoDB shelter ID for PostgreSQL shelter ID {PostgreSqlShelterId}", postgresqlShelterId);
            return null;
        }
    }

    /// <summary>
    /// Converts a DynamoDB item to a Pet object
    /// </summary>
    private static Pet ConvertDynamoDbItemToPet(Dictionary<string, AttributeValue> item)
    {
        var petId = Guid.Parse(item["PetId"].S);
        var petPostgreSqlId = int.Parse(item["PetPostgreSqlId"].N);
        var weight = item.TryGetValue("Weight", out var weightValue) ? (decimal?)decimal.Parse(weightValue.N) : null;
        var color = item.TryGetValue("Color", out var colorValue) ? colorValue.S : string.Empty;
        var specialNeeds = item.TryGetValue("SpecialNeeds", out var specialNeedsValue) ? specialNeedsValue.S : string.Empty;

        return new Pet
        {
            PetId = petId,
            PetPostgreSqlId = petPostgreSqlId,
            Weight = weight,
            Color = color,
            SpecialNeeds = specialNeeds
        };
    }

    /// <summary>
    /// Converts a Pet object to a DynamoDB item
    /// </summary>
    private static Dictionary<string, AttributeValue> ConvertPetToDynamoDbItem(Pet pet)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PetId", new AttributeValue { S = pet.PetId.ToString() } },
            { "PetPostgreSqlId", new AttributeValue { N = pet.PetPostgreSqlId.ToString() } },
            { "Color", new AttributeValue { S = pet.Color } },
            { "SpecialNeeds", new AttributeValue { S = pet.SpecialNeeds } },
            { "CreatedAt", new AttributeValue { S = pet.CreatedAt?.ToString("o") } },
            { "ShelterId", new AttributeValue { S = pet.ShelterId.ToString() } }
        };

        // Add Weight if it has a value
        if (pet.Weight.HasValue)
        {
            item["Weight"] = new AttributeValue { N = pet.Weight.Value.ToString() };
        }

        return item;
    }

    public async Task<PresignedUrlResponse> GenerateUploadUrl(Guid petId, string fileName, string contentType, long fileSizeBytes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Content type must be provided", nameof(contentType));
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("File name must have an extension", nameof(fileName));
            }

            _logger.LogInformation("Generating upload URL for pet {PetId} with extension {Extension}", petId, extension);

            // Get the pet from DynamoDB to get PetPostgreSqlId
            var dynamoDbPetResponse = await GetPetById(petId);
            if (!dynamoDbPetResponse.Success || dynamoDbPetResponse.Pet == null)
            {
                _logger.LogWarning("Pet not found when generating upload URL: {PetId}", petId);
                return new PresignedUrlResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            // Update the pet's MainImageFileExtension in PostgreSQL
            var postgresPet = await _dbContext.Pets.FindAsync(dynamoDbPetResponse.Pet.PetPostgreSqlId);
            if (postgresPet == null)
            {
                _logger.LogWarning("PostgreSQL pet not found when generating upload URL: {PostgresPetId}", dynamoDbPetResponse.Pet.PetPostgreSqlId);
                return new PresignedUrlResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found in database"
                };
            }

            postgresPet.MainImageFileExtension = extension;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated PostgreSQL pet {PostgresPetId} with main image file extension {Extension}", dynamoDbPetResponse.Pet.PetPostgreSqlId, extension);

            // Get shelter ID using the proper lookup method for cache invalidation
            var shelterId = await GetShelterIdForPet(dynamoDbPetResponse.Pet);
            if (shelterId.HasValue)
            {
                InvalidatePetCountCache(shelterId.Value);
                InvalidatePetStatisticsCache(shelterId.Value);
            }

            // Generate a presigned URL for uploading pet images
            var presignedUrlResponse = await _mediaUploadService.GeneratePresignedUrlAsync(new PresignedUrlRequest
            {
                BucketName = _bucketName,
                Key = $"pets/{petId}/main-image{extension}",
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
            });

            _logger.LogInformation("Successfully generated upload URL for pet {PetId}", petId);

            return presignedUrlResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload URL for pet {PetId}", petId);
            return new PresignedUrlResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate upload URL: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets download presigned URLs for main images of multiple pets
    /// </summary>
    /// <param name="petIds">List of pet IDs to get image URLs for</param>
    /// <returns>Dictionary of pet IDs to their download presigned URLs</returns>
    public async Task<PetImageDownloadUrlsResponse> GetPetImageDownloadUrls(List<Guid> petIds)
    {
        try
        {
            _logger.LogInformation("Getting download presigned URLs for {PetCount} pets", petIds.Count);

            if (petIds == null || petIds.Count == 0)
            {
                return new PetImageDownloadUrlsResponse
                {
                    Success = true,
                    PetImageUrls = []
                };
            }

            // Limit the number of pets to process at once to avoid performance issues
            if (petIds.Count > 100)
            {
                return new PetImageDownloadUrlsResponse
                {
                    Success = false,
                    ErrorMessage = "Maximum 100 pet IDs allowed per request"
                };
            }

            var result = new Dictionary<Guid, string?>();

            // Process each pet ID
            foreach (var petId in petIds)
            {
                // For this method, we don't have the file extension, so we'll try common extensions
                // This is a fallback method - the new method with extensions is preferred
                var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                string? foundUrl = null;

                foreach (var ext in extensions)
                {
                    var imageS3Key = $"pets/{petId}/main-image{ext}";
                    var presignedUrl = await _mediaUploadService.GenerateDownloadPresignedUrlAsync(_bucketName, imageS3Key);
                    if (!string.IsNullOrEmpty(presignedUrl))
                    {
                        foundUrl = presignedUrl;
                        break;
                    }
                }

                result[petId] = foundUrl;
            }

            _logger.LogInformation("Successfully generated download URLs for {PetCount} pets", petIds.Count);

            return new PetImageDownloadUrlsResponse
            {
                Success = true,
                PetImageUrls = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download presigned URLs for pets");
            return new PetImageDownloadUrlsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to get download presigned URLs: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets download presigned URLs for main images of multiple pets with their file extensions
    /// </summary>
    /// <param name="request">Request containing pet IDs and their file extensions</param>
    /// <returns>Dictionary of pet IDs to their download presigned URLs</returns>
    public async Task<PetImageDownloadUrlsResponse> GetPetImageDownloadUrls(GetPetImageDownloadUrlsRequest request)
    {
        try
        {
            _logger.LogInformation("Getting download presigned URLs for {PetCount} pets with extensions", request.PetRequests.Count);

            if (request == null || request.PetRequests == null || request.PetRequests.Count == 0)
            {
                return new PetImageDownloadUrlsResponse
                {
                    Success = true,
                    PetImageUrls = []
                };
            }

            // Limit the number of pets to process at once to avoid performance issues
            if (request.PetRequests.Count > 100)
            {
                return new PetImageDownloadUrlsResponse
                {
                    Success = false,
                    ErrorMessage = "Maximum 100 pet requests allowed per request"
                };
            }

            var result = new Dictionary<Guid, string?>();

            // Process each pet request
            foreach (var petRequest in request.PetRequests)
            {
                var extension = string.IsNullOrEmpty(petRequest.MainImageFileExtension)
                    ? throw new InvalidOperationException("MainImageFileExtension must be provided")
                    : petRequest.MainImageFileExtension;

                // Ensure extension starts with a dot
                if (!extension.StartsWith('.'))
                {
                    extension = '.' + extension;
                }

                var imageS3Key = $"pets/{petRequest.PetId}/main-image{extension}";
                var presignedUrl = await _mediaUploadService.GenerateDownloadPresignedUrlAsync(_bucketName, imageS3Key);
                result[petRequest.PetId] = presignedUrl;
            }

            _logger.LogInformation("Successfully generated download URLs for {PetCount} pets with extensions", request.PetRequests.Count);

            return new PetImageDownloadUrlsResponse
            {
                Success = true,
                PetImageUrls = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download presigned URLs for pets with extensions");
            return new PetImageDownloadUrlsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to get download presigned URLs: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the total pet count for a shelter with caching support
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="request">The request containing filters</param>
    /// <returns>Total pet count</returns>
    private async Task<int> GetTotalPetCountWithCache(Guid shelterId, GetPetsRequest request)
    {
        // Generate cache key based on shelter ID and filters
        var cacheKey = GeneratePetCountCacheKey(shelterId, request);

        // Try to get count from cache first
        if (_memoryCache.TryGetValue(cacheKey, out int cachedCount))
        {
            _logger.LogDebug("Retrieved pet count from cache for shelter {ShelterId}: {Count}", shelterId, cachedCount);
            return cachedCount;
        }

        // If not in cache, calculate count with paginated queries
        _logger.LogDebug("Pet count not in cache for shelter {ShelterId}, calculating...", shelterId);
        var totalCount = await CalculateTotalPetCount(shelterId, request);

        // Cache the result for 5 minutes
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(cacheKey, totalCount, cacheOptions);
        _logger.LogDebug("Cached pet count for shelter {ShelterId}: {Count}", shelterId, totalCount);

        return totalCount;
    }

    /// <summary>
    /// Calculates the total pet count with paginated queries (including filters)
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="request">The request containing filters</param>
    /// <returns>Total pet count</returns>
    private async Task<int> CalculateTotalPetCount(Guid shelterId, GetPetsRequest request)
    {
        var totalCount = 0;
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        // Build filter conditions (reuse the same logic from the main method)
        var filterConditions = new List<string>();
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":shelterId", new AttributeValue { S = shelterId.ToString() } }
        };
        var expressionAttributeNames = new Dictionary<string, string>();

        if (request.Status.HasValue)
        {
            filterConditions.Add("#status = :status");
            expressionAttributeValues[":status"] = new AttributeValue { S = request.Status.Value.GetAmbientValue<string>() };
            expressionAttributeNames["#status"] = "Status";
        }

        if (!string.IsNullOrWhiteSpace(request.Species))
        {
            filterConditions.Add("#species = :species");
            expressionAttributeValues[":species"] = new AttributeValue { S = request.Species };
            expressionAttributeNames["#species"] = "Species";
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            filterConditions.Add("contains(#petName, :petName)");
            expressionAttributeValues[":petName"] = new AttributeValue { S = request.Name };
            expressionAttributeNames["#petName"] = "Name";
        }

        if (!string.IsNullOrWhiteSpace(request.Breed))
        {
            filterConditions.Add("contains(#breed, :breed)");
            expressionAttributeValues[":breed"] = new AttributeValue { S = request.Breed };
            expressionAttributeNames["#breed"] = "Breed";
        }

        do
        {
            var countQuery = new QueryRequest
            {
                TableName = _tableName,
                IndexName = _shelterIdCreatedAtIndex,
                KeyConditionExpression = "ShelterId = :shelterId",
                ExpressionAttributeValues = expressionAttributeValues,
                Select = Select.COUNT
            };

            // Apply filters if any
            if (filterConditions.Count > 0)
            {
                countQuery.FilterExpression = string.Join(" AND ", filterConditions);
                countQuery.ExpressionAttributeNames = expressionAttributeNames;
            }

            // Add pagination for the count query if needed
            if (lastEvaluatedKey != null)
            {
                countQuery.ExclusiveStartKey = lastEvaluatedKey;
            }

            var countResponse = await _dynamoDbClient.QueryAsync(countQuery);
            totalCount += countResponse.Count ?? 0;
            lastEvaluatedKey = countResponse.LastEvaluatedKey;

        } while (lastEvaluatedKey != null && lastEvaluatedKey.Count > 0);

        return totalCount;
    }

    /// <summary>
    /// Generates a cache key for pet count based on shelter ID and filters
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="request">The request containing filters</param>
    /// <returns>Cache key string</returns>
    private static string GeneratePetCountCacheKey(Guid shelterId, GetPetsRequest request)
    {
        var keyParts = new List<string>
        {
            "pet-count",
            shelterId.ToString()
        };

        if (request.Status.HasValue)
        {
            keyParts.Add($"status:{request.Status.Value.GetAmbientValue<string>()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Species))
        {
            keyParts.Add($"species:{request.Species}");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            keyParts.Add($"name:{request.Name}");
        }

        if (!string.IsNullOrWhiteSpace(request.Breed))
        {
            keyParts.Add($"breed:{request.Breed}");
        }

        return string.Join(":", keyParts);
    }

    /// <summary>
    /// Invalidates all cached pet counts for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    private void InvalidatePetCountCache(Guid shelterId)
    {
        try
        {
            // Since we can't easily enumerate cache keys in IMemoryCache,
            // we'll remove common cache patterns for this shelter
            var baseKey = $"pet-count:{shelterId}";

            // Remove the base cache entry (no filters)
            _memoryCache.Remove(baseKey);

            // Remove common filter combinations
            var commonStatuses = new[] { "Available", "Adopted", "Pending", "Medical" };
            var commonSpecies = new[] { "Dog", "Cat", "Bird", "Rabbit", "Other" };

            foreach (var status in commonStatuses)
            {
                _memoryCache.Remove($"{baseKey}:status:{status}");

                foreach (var species in commonSpecies)
                {
                    _memoryCache.Remove($"{baseKey}:status:{status}:species:{species}");
                    _memoryCache.Remove($"{baseKey}:species:{species}");
                }
            }

            // For more comprehensive cache invalidation in production, consider:
            // 1. Using a cache tagging system
            // 2. Maintaining a list of active cache keys per shelter
            // 3. Using Redis with pattern-based removal
            // 4. Implementing a cache notification system

            _logger.LogDebug("Invalidated pet count cache for shelter {ShelterId}", shelterId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate pet count cache for shelter {ShelterId}", shelterId);
        }
    }

    /// <summary>
    /// Gets pet statistics for a specific shelter with caching
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>Pet statistics including total and adopted counts</returns>
    public async Task<ShelterPetStatisticsResponse> GetShelterPetStatistics(Guid shelterId)
    {
        try
        {
            _logger.LogInformation("Getting pet statistics for shelter {ShelterId}", shelterId);

            // Generate cache key for statistics
            var cacheKey = $"shelter-pet-stats:{shelterId}";

            // Try to get statistics from cache first
            if (_memoryCache.TryGetValue(cacheKey, out ShelterPetStatisticsResponse? cachedStats))
            {
                _logger.LogDebug("Retrieved pet statistics from cache for shelter {ShelterId}", shelterId);
                cachedStats!.FromCache = true;
                return cachedStats;
            }

            // If not in cache, calculate statistics
            _logger.LogDebug("Pet statistics not in cache for shelter {ShelterId}, calculating...", shelterId);
            var statistics = await CalculateShelterPetStatistics(shelterId);

            // Cache the result for 10 minutes
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(cacheKey, statistics, cacheOptions);
            _logger.LogDebug("Cached pet statistics for shelter {ShelterId}", shelterId);

            statistics.FromCache = false;
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pet statistics for shelter {ShelterId}", shelterId);
            return new ShelterPetStatisticsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve pet statistics: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Calculates pet statistics for a shelter by querying DynamoDB
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>Pet statistics</returns>
    private async Task<ShelterPetStatisticsResponse> CalculateShelterPetStatistics(Guid shelterId)
    {
        var statistics = new ShelterPetStatisticsResponse
        {
            Success = true,
            LastUpdated = DateTime.UtcNow
        };

        // Get total count for all pets (no status filter)
        var totalPetsRequest = new GetPetsRequest(); // No filters for total count
        statistics.TotalPets = await CalculateTotalPetCount(shelterId, totalPetsRequest);

        // Get count for adopted pets only
        statistics.AdoptedPets = await CalculatePetCountByStatus(shelterId, PetStatus.Adopted);

        return statistics;
    }

    /// <summary>
    /// Calculates the count of pets by status for a shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="status">The pet status to count</param>
    /// <returns>Count of pets with the specified status</returns>
    private async Task<int> CalculatePetCountByStatus(Guid shelterId, PetStatus status)
    {
        var totalCount = 0;
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var countQuery = new QueryRequest
            {
                TableName = _tableName,
                IndexName = _shelterIdCreatedAtIndex,
                KeyConditionExpression = "ShelterId = :shelterId",
                FilterExpression = "#status = :status",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":shelterId", new AttributeValue { S = shelterId.ToString() } },
                    { ":status", new AttributeValue { S = status.GetAmbientValue<string>() } }
                },
                Select = Select.COUNT
            };

            // Add pagination for the count query if needed
            if (lastEvaluatedKey != null)
            {
                countQuery.ExclusiveStartKey = lastEvaluatedKey;
            }

            var countResponse = await _dynamoDbClient.QueryAsync(countQuery);
            totalCount += countResponse.Count ?? 0;
            lastEvaluatedKey = countResponse.LastEvaluatedKey;

        } while (lastEvaluatedKey != null && lastEvaluatedKey.Count > 0);

        return totalCount;
    }

    /// <summary>
    /// Invalidates shelter pet statistics cache
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    private void InvalidatePetStatisticsCache(Guid shelterId)
    {
        try
        {
            var cacheKey = $"shelter-pet-stats:{shelterId}";
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug("Invalidated pet statistics cache for shelter {ShelterId}", shelterId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate pet statistics cache for shelter {ShelterId}", shelterId);
        }
    }

    public async Task<GetPetMediaResponse> GetPetMedia(Guid petId)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = _petMediaFilesTableName,
                KeyConditionExpression = "PetId = :petId",
                FilterExpression = "attribute_not_exists(#status) OR #status <> :pendingStatus",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":petId", new AttributeValue { S = petId.ToString() } },
                    { ":pendingStatus", new AttributeValue { S = "Pending" } }
                },
                ScanIndexForward = true // Order by sort key ascending (DisplayOrder)
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            var mediaFiles = new List<PetMediaFile>();

            foreach (var item in response.Items)
            {
                mediaFiles.Add(new PetMediaFile
                {
                    MediaFileId = Guid.Parse(item["MediaFileId"].S),
                    PetId = Guid.Parse(item["PetId"].S),
                    FileName = item.TryGetValue("FileName", out AttributeValue? fileName) ? fileName.S : "",
                    FileExtension = item.TryGetValue("FileExtension", out AttributeValue? fileExtension) ? fileExtension.S : "",
                    FileType = item.TryGetValue("FileType", out AttributeValue? fileType)
                        ? Enum.Parse<MediaFileType>(fileType.S, true)
                        : null,
                    ContentType = item.TryGetValue("ContentType", out AttributeValue? contentType) ? contentType.S : "",
                    FileSizeBytes = item.TryGetValue("FileSizeBytes", out AttributeValue? fileSize)
                        ? long.Parse(fileSize.N)
                        : null,
                    S3Key = item.TryGetValue("S3Key", out AttributeValue? s3Key) ? s3Key.S : "",
                    DisplayOrder = int.Parse(item["DisplayOrder"].N),
                    UploadedAt = DateTime.Parse(item["UploadedAt"].S)
                });
            }

            // Convert to response models with download URLs
            var images = new List<PetMediaFileResponse>();
            var videos = new List<PetMediaFileResponse>();
            var documents = new List<PetMediaFileResponse>();

            foreach (var mediaFile in mediaFiles)
            {
                var downloadUrl = await _mediaUploadService.GenerateDownloadPresignedUrlAsync(_bucketName, mediaFile.S3Key);

                var responseItem = new PetMediaFileResponse
                {
                    MediaFileId = mediaFile.MediaFileId,
                    FileName = mediaFile.FileName,
                    FileExtension = mediaFile.FileExtension,
                    FileType = mediaFile.FileType,
                    ContentType = mediaFile.ContentType,
                    FileSizeBytes = mediaFile.FileSizeBytes,
                    DownloadUrl = downloadUrl ?? "",
                    UploadedAt = mediaFile.UploadedAt,
                    DisplayOrder = mediaFile.DisplayOrder
                };

                switch (mediaFile.FileType)
                {
                    case MediaFileType.Image:
                        images.Add(responseItem);
                        break;
                    case MediaFileType.Video:
                        videos.Add(responseItem);
                        break;
                    case MediaFileType.Document:
                        documents.Add(responseItem);
                        break;
                }
            }

            return new GetPetMediaResponse
            {
                Success = true,
                Images = [.. images.OrderBy(i => i.DisplayOrder)],
                Videos = [.. videos.OrderBy(v => v.DisplayOrder)],
                Documents = [.. documents.OrderBy(d => d.DisplayOrder)]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media for pet {PetId}", petId);
            return new GetPetMediaResponse
            {
                Success = false,
                ErrorMessage = "Failed to retrieve pet media"
            };
        }
    }

    public async Task<MediaFileUploadResponse> GenerateMediaUploadUrls(UploadMediaFilesRequest request)
    {
        try
        {
            // Get current max display order for this pet first
            var existingMedia = await GetPetMedia(request.PetId);
            var maxDisplayOrder = 0;

            if (existingMedia.Images.Count != 0
                || existingMedia.Videos.Count != 0
                || existingMedia.Documents.Count != 0
                )
            {
                var allExisting = existingMedia.Images.Cast<PetMediaFileResponse>()
                    .Concat(existingMedia.Videos)
                    .Concat(existingMedia.Documents);
                maxDisplayOrder = allExisting.Max(m => m.DisplayOrder);
            }

            var uploadUrls = new List<MediaFileUploadUrlResponse>();
            var batchRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_petMediaFilesTableName] = []
                }
            };

            foreach (var fileRequest in request.MediaFiles)
            {
                var mediaFileId = Guid.NewGuid();
                var fileExtension = Path.GetExtension(fileRequest.FileName ?? "").ToLowerInvariant();
                var s3Key = $"pets/{request.PetId}/media/{mediaFileId}{fileExtension}";
                var fileType = fileRequest.FileType != default ? fileRequest.FileType : DetermineFileType(fileExtension);
                var displayOrder = ++maxDisplayOrder; // Increment for each new file

                // Use the existing media upload service to generate presigned URL
                var presignedRequest = new PresignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key,
                    ContentType = fileRequest.ContentType ?? "",
                    FileSizeBytes = fileRequest.FileSizeBytes
                };

                var presignedResponse = await _mediaUploadService.GeneratePresignedUrlAsync(presignedRequest);

                // Store media file metadata in DynamoDB with pending status
                var mediaFileItem = new Dictionary<string, AttributeValue>
                {
                    ["MediaFileId"] = new AttributeValue { S = mediaFileId.ToString() },
                    ["PetId"] = new AttributeValue { S = request.PetId.ToString() },
                    ["FileName"] = new AttributeValue { S = fileRequest.FileName ?? "" },
                    ["FileExtension"] = new AttributeValue { S = fileExtension },
                    ["FileType"] = new AttributeValue { S = fileType.ToString() },
                    ["ContentType"] = new AttributeValue { S = fileRequest.ContentType ?? "" },
                    ["FileSizeBytes"] = new AttributeValue { N = fileRequest.FileSizeBytes.ToString() },
                    ["S3Key"] = new AttributeValue { S = s3Key },
                    ["Status"] = new AttributeValue { S = "Pending" }, // Mark as pending until confirmed
                    ["CreatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                    ["DisplayOrder"] = new AttributeValue { N = displayOrder.ToString() }
                };

                batchRequest.RequestItems[_petMediaFilesTableName].Add(new WriteRequest
                {
                    PutRequest = new PutRequest { Item = mediaFileItem }
                });

                uploadUrls.Add(new MediaFileUploadUrlResponse
                {
                    MediaFileId = mediaFileId,
                    FileName = fileRequest.FileName ?? "",
                    PresignedUrl = presignedResponse.PresignedUrl ?? "",
                    S3Key = s3Key,
                    ExpiresAt = presignedResponse.ExpiresAt ?? DateTime.UtcNow.AddMinutes(30)
                });
            }

            // Store all media file metadata in DynamoDB
            if (batchRequest.RequestItems[_petMediaFilesTableName].Count > 0)
            {
                await _dynamoDbClient.BatchWriteItemAsync(batchRequest);
                _logger.LogInformation("Stored {FileCount} pending media files for pet {PetId}",
                    batchRequest.RequestItems[_petMediaFilesTableName].Count, request.PetId);
            }

            return new MediaFileUploadResponse
            {
                Success = true,
                UploadUrls = uploadUrls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload URLs for pet {PetId}", request.PetId);
            return new MediaFileUploadResponse
            {
                Success = false,
                ErrorMessage = "Failed to generate upload URLs"
            };
        }
    }

    public async Task<GetPetMediaResponse> ConfirmMediaUploads(Guid petId, List<Guid> mediaFileIds)
    {
        try
        {
            // Query for pending media files for this pet
            var queryRequest = new QueryRequest
            {
                TableName = _petMediaFilesTableName,
                KeyConditionExpression = "PetId = :petId",
                FilterExpression = "#status = :pendingStatus AND MediaFileId IN (" +
                    string.Join(",", mediaFileIds.Select((_, index) => $":mediaFileId{index}")) + ")",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":petId", new AttributeValue { S = petId.ToString() } },
                    { ":pendingStatus", new AttributeValue { S = "Pending" } }
                }
            };

            // Add media file IDs to expression attribute values
            for (int i = 0; i < mediaFileIds.Count; i++)
            {
                queryRequest.ExpressionAttributeValues[$":mediaFileId{i}"] = new AttributeValue { S = mediaFileIds[i].ToString() };
            }

            var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);
            var pendingFiles = queryResponse.Items;

            if (pendingFiles.Count != mediaFileIds.Count)
            {
                _logger.LogWarning("Not all pending media files found for confirmation. Expected: {ExpectedCount}, Found: {FoundCount}",
                    mediaFileIds.Count, pendingFiles.Count);
            }

            var batchUpdateRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_petMediaFilesTableName] = []
                }
            };

            foreach (var fileItem in pendingFiles)
            {
                // Create updated item with confirmed status, keeping existing DisplayOrder
                var updatedItem = new Dictionary<string, AttributeValue>(fileItem)
                {
                    ["UploadedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                };

                // Remove the Status field to make it a confirmed file (no status = confirmed)
                updatedItem.Remove("Status");

                batchUpdateRequest.RequestItems[_petMediaFilesTableName].Add(new WriteRequest
                {
                    PutRequest = new PutRequest { Item = updatedItem }
                });
            }

            if (batchUpdateRequest.RequestItems[_petMediaFilesTableName].Count > 0)
            {
                await _dynamoDbClient.BatchWriteItemAsync(batchUpdateRequest);
                _logger.LogInformation("Confirmed {FileCount} media file uploads for pet {PetId}",
                    batchUpdateRequest.RequestItems[_petMediaFilesTableName].Count, petId);
            }

            // Return updated media list
            return await GetPetMedia(petId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm media uploads for pet {PetId}", petId);
            return new GetPetMediaResponse
            {
                Success = false,
                ErrorMessage = "Failed to confirm media uploads"
            };
        }
    }

    public async Task<DeleteMediaFilesResponse> DeleteMediaFiles(Guid petId, DeleteMediaFilesRequest request)
    {
        try
        {
            // Get the media files to delete first to know their S3 keys using GSI
            var mediaFilesToDelete = new List<Dictionary<string, AttributeValue>>();

            foreach (var mediaFileId in request.MediaFileIds)
            {
                var queryRequest = new QueryRequest
                {
                    TableName = _petMediaFilesTableName,
                    IndexName = "MediaFileIdIndex",
                    KeyConditionExpression = "MediaFileId = :mediaFileId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":mediaFileId", new AttributeValue { S = mediaFileId.ToString() } }
                    }
                };

                var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);
                mediaFilesToDelete.AddRange(queryResponse.Items);
            }

            // Delete files from S3 - note: we'd need to implement this in the media service
            // For now, we'll just log it
            foreach (var item in mediaFilesToDelete)
            {
                var s3Key = item["S3Key"].S;
                _logger.LogInformation("Would delete S3 file: {S3Key}", s3Key);
                await _mediaUploadService.DeleteFileAsync(_bucketName, s3Key);
            }

            // Delete records from DynamoDB using the keys from GSI query results
            var batchDeleteRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_petMediaFilesTableName] = []
                }
            };

            foreach (var item in mediaFilesToDelete)
            {
                // Use the actual table keys (PetId as partition key, DisplayOrder as sort key)
                var deleteRequest = new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PetId"] = item["PetId"],
                            ["DisplayOrder"] = item["DisplayOrder"]
                        }
                    }
                };

                batchDeleteRequest.RequestItems[_petMediaFilesTableName].Add(deleteRequest);
            }

            await _dynamoDbClient.BatchWriteItemAsync(batchDeleteRequest);

            return new DeleteMediaFilesResponse
            {
                Success = true,
                DeletedCount = request.MediaFileIds.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media files for pet {PetId}: {MediaFileIds}", petId, string.Join(", ", request.MediaFileIds));
            return new DeleteMediaFilesResponse
            {
                Success = false,
                ErrorMessage = "Failed to delete media files"
            };
        }
    }

    public async Task<GetPetMediaResponse> ReorderMediaFiles(Guid petId, Dictionary<Guid, int> mediaFileOrders)
    {
        try
        {
            var batchRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_petMediaFilesTableName] = []
                }
            };

            foreach (var kvp in mediaFileOrders)
            {
                var updateRequest = new WriteRequest
                {
                    PutRequest = new PutRequest
                    {
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["MediaFileId"] = new AttributeValue { S = kvp.Key.ToString() },
                            ["DisplayOrder"] = new AttributeValue { N = kvp.Value.ToString() }
                        }
                    }
                };

                batchRequest.RequestItems[_petMediaFilesTableName].Add(updateRequest);
            }

            await _dynamoDbClient.BatchWriteItemAsync(batchRequest);

            // Return updated media list
            return await GetPetMedia(petId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder media files for pet {PetId}", petId);
            return new GetPetMediaResponse
            {
                Success = false,
                ErrorMessage = "Failed to reorder media files"
            };
        }
    }

    private MediaFileType DetermineFileType(string fileExtension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" };

        if (imageExtensions.Contains(fileExtension))
            return MediaFileType.Image;

        if (videoExtensions.Contains(fileExtension))
            return MediaFileType.Video;

        return MediaFileType.Document;
    }

    /// <summary>
    /// Cleans up pending media files that are older than the specified age
    /// This can be called periodically to remove stale upload records
    /// </summary>
    /// <param name="olderThanHours">Remove pending files older than this many hours (default: 24)</param>
    /// <returns>Number of files cleaned up</returns>
    public async Task<int> CleanupPendingMediaFiles(int olderThanHours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-olderThanHours);
            var scanRequest = new ScanRequest
            {
                TableName = _petMediaFilesTableName,
                FilterExpression = "#status = :pendingStatus AND #createdAt < :cutoffTime",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" },
                    { "#createdAt", "CreatedAt" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pendingStatus", new AttributeValue { S = "Pending" } },
                    { ":cutoffTime", new AttributeValue { S = cutoffTime.ToString("O") } }
                },
                ProjectionExpression = "MediaFileId, S3Key"
            };

            var response = await _dynamoDbClient.ScanAsync(scanRequest);

            if (response.Items.Count == 0)
            {
                return 0;
            }

            // Delete the stale pending records
            var batchDeleteRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_petMediaFilesTableName] = [.. response.Items.Select(item => new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>
                            {
                                ["MediaFileId"] = item["MediaFileId"]
                            }
                        }
                    })]
                }
            };

            await _dynamoDbClient.BatchWriteItemAsync(batchDeleteRequest);

            _logger.LogInformation("Cleaned up {Count} pending media files older than {Hours} hours",
                response.Items.Count, olderThanHours);

            return response.Items.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup pending media files");
            return 0;
        }
    }

    /// <summary>
    /// Gets all available pet species
    /// </summary>
    /// <returns>List of all pet species</returns>
    public async Task<GetPetSpeciesResponse> GetAllPetSpecies()
    {
        try
        {
            _logger.LogInformation("Getting all pet species");

            var species = await _dbContext.PetSpecies
                .OrderBy(s => s.SpeciesId)
                .Select(s => new PetSpeciesDto
                {
                    SpeciesId = s.SpeciesId,
                    Name = s.Name
                })
                .ToListAsync();

            _logger.LogInformation("Successfully retrieved {SpeciesCount} pet species", species.Count);

            return new GetPetSpeciesResponse
            {
                Success = true,
                Species = species
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pet species");
            return new GetPetSpeciesResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve pet species: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets all breeds for a specific species
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>List of breeds for the species</returns>
    public async Task<GetPetBreedsResponse> GetBreedsBySpeciesId(int speciesId)
    {
        try
        {
            _logger.LogInformation("Getting breeds for species ID: {SpeciesId}", speciesId);

            // First check if the species exists
            var speciesExists = await _dbContext.PetSpecies
                .AnyAsync(s => s.SpeciesId == speciesId);

            if (!speciesExists)
            {
                _logger.LogWarning("Species with ID {SpeciesId} not found", speciesId);
                return new GetPetBreedsResponse
                {
                    Success = false,
                    ErrorMessage = $"Species with ID {speciesId} not found"
                };
            }

            var breeds = await _dbContext.PetBreeds
                .Where(b => b.SpeciesId == speciesId)
                .OrderBy(b => b.SpeciesId)
                .Select(b => new PetBreedDto
                {
                    BreedId = b.BreedId,
                    Name = b.Name,
                    SpeciesId = b.SpeciesId
                })
                .ToListAsync();

            _logger.LogInformation("Successfully retrieved {BreedCount} breeds for species ID {SpeciesId}", breeds.Count, speciesId);

            return new GetPetBreedsResponse
            {
                Success = true,
                Breeds = breeds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get breeds for species ID: {SpeciesId}", speciesId);
            return new GetPetBreedsResponse
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve breeds for species: {ex.Message}"
            };
        }
    }
}

