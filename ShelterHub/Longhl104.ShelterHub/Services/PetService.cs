using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Longhl104.ShelterHub.Models;
using Longhl104.PawfectMatch.Extensions;
using Microsoft.Extensions.Caching.Memory;

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
}

/// <summary>
/// Service for managing pet operations with DynamoDB
/// </summary>
public class PetService : IPetService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PetService> _logger;
    private readonly IMediaService _mediaUploadService;
    private readonly IMemoryCache _memoryCache;
    private readonly string _tableName;
    private readonly string _bucketName;
    private readonly string _shelterIdCreatedAtIndex = "ShelterIdCreatedAtIndex";

    public PetService(
        IAmazonDynamoDB dynamoDbClient,
        IHostEnvironment environment,
        ILogger<PetService> logger,
        IMediaService mediaUploadService,
        IMemoryCache memoryCache
        )
    {
        _dynamoDbClient = dynamoDbClient;
        _environment = environment;
        _logger = logger;
        _mediaUploadService = mediaUploadService;
        _memoryCache = memoryCache;
        _tableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pets";
        _bucketName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pet-media";
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
                query.ExpressionAttributeValues[":petName"] = new AttributeValue { S = request.Name };
            }

            if (!string.IsNullOrWhiteSpace(request.Breed))
            {
                filterConditions.Add("contains(#breed, :breed)");
                query.ExpressionAttributeValues[":breed"] = new AttributeValue { S = request.Breed };
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

            var pet = new Pet
            {
                PetId = Guid.NewGuid(),
                Name = request.Name.ToLowerInvariant(),
                Species = request.Species,
                Breed = request.Breed.ToLowerInvariant(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Description = request.Description,
                ShelterId = shelterId,
                Status = PetStatus.Available,
                CreatedAt = DateTime.UtcNow,
            };

            var putRequest = new PutItemRequest
            {
                TableName = _tableName,
                Item = ConvertPetToDynamoDbItem(pet)
            };

            await _dynamoDbClient.PutItemAsync(putRequest);

            // Invalidate cache for this shelter
            InvalidatePetCountCache(shelterId);
            InvalidatePetStatisticsCache(shelterId);

            _logger.LogInformation("Successfully created pet with ID: {PetId}", pet.PetId);

            return new PetResponse
            {
                Success = true,
                Pet = pet
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

            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                },
                UpdateExpression = "SET #name = :name, #species = :species, #breed = :breed, #dateOfBirth = :dateOfBirth, #gender = :gender, #description = :description, #adoptionFee = :adoptionFee, #color = :color, #isSpayedNeutered = :isSpayedNeutered, #isHouseTrained = :isHouseTrained, #isGoodWithKids = :isGoodWithKids, #isGoodWithPets = :isGoodWithPets, #specialNeeds = :specialNeeds, #status = :status" + (request.Weight.HasValue ? ", #weight = :weight" : ""),
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
                    { "#isHouseTrained", "IsHouseTrained" },
                    { "#isGoodWithKids", "IsGoodWithKids" },
                    { "#isGoodWithPets", "IsGoodWithPets" },
                    { "#specialNeeds", "SpecialNeeds" },
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":name", new AttributeValue { S = request.Name.ToLowerInvariant() } },
                    { ":species", new AttributeValue { S = request.Species } },
                    { ":breed", new AttributeValue { S = request.Breed.ToLowerInvariant() } },
                    { ":dateOfBirth", new AttributeValue { S = request.DateOfBirth.ToString("yyyy-MM-dd") } },
                    { ":gender", new AttributeValue { S = request.Gender } },
                    { ":description", new AttributeValue { S = request.Description } },
                    { ":adoptionFee", new AttributeValue { N = request.AdoptionFee.ToString("F2") } },
                    { ":color", new AttributeValue { S = request.Color } },
                    { ":isSpayedNeutered", new AttributeValue { BOOL = request.IsSpayedNeutered } },
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

            if (!response.Attributes.Any())
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
            InvalidatePetCountCache(updatedPet.ShelterId);
            InvalidatePetStatisticsCache(updatedPet.ShelterId);

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

            if (!response.Attributes.Any())
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
            InvalidatePetCountCache(updatedPet.ShelterId);
            InvalidatePetStatisticsCache(updatedPet.ShelterId);

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
            InvalidatePetCountCache(deletedPet.ShelterId);
            InvalidatePetStatisticsCache(deletedPet.ShelterId);

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
    /// Converts a DynamoDB item to a Pet object
    /// </summary>
    private static Pet ConvertDynamoDbItemToPet(Dictionary<string, AttributeValue> item)
    {
        return new Pet
        {
            PetId = Guid.Parse(item["PetId"].S),
            Name = item["Name"].S,
            Species = item["Species"].S,
            Breed = item["Breed"].S,
            DateOfBirth = item.TryGetValue("DateOfBirth", out var dob) ? DateOnly.Parse(dob.S) : DateOnly.MinValue,
            Gender = item["Gender"].S,
            Description = item.TryGetValue("Description", out var desc) ? desc.S : string.Empty,
            ShelterId = Guid.Parse(item["ShelterId"].S),
            Status = Enum.Parse<PetStatus>(item["Status"].S),
            CreatedAt = DateTime.Parse(item["CreatedAt"].S),
            MainImageFileExtension = item.TryGetValue("MainImageFileExtension", out var ext) ? ext.S : null
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
            { "Name", new AttributeValue { S = pet.Name } },
            { "Species", new AttributeValue { S = pet.Species } },
            { "Breed", new AttributeValue { S = pet.Breed } },
            { "DateOfBirth", new AttributeValue { S = pet.DateOfBirth.ToString("O") } },
            { "Gender", new AttributeValue { S = pet.Gender } },
            { "Description", new AttributeValue { S = pet.Description } },
            { "ShelterId", new AttributeValue { S = pet.ShelterId.ToString() } },
            { "Status", new AttributeValue { S = pet.Status.GetAmbientValue<string>() } },
            { "CreatedAt", new AttributeValue { S = pet.CreatedAt.ToString("O") } }
        };

        // Add MainImageFileExtension if it exists
        if (!string.IsNullOrWhiteSpace(pet.MainImageFileExtension))
        {
            item["MainImageFileExtension"] = new AttributeValue { S = pet.MainImageFileExtension };
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

            // Update the pet's MainImageFileExtension in DynamoDB
            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PetId", new AttributeValue { S = petId.ToString() } }
                },
                UpdateExpression = "SET MainImageFileExtension = :extension",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":extension", new AttributeValue { S = extension } }
                },
                ConditionExpression = "attribute_exists(PetId)", // Ensure the pet exists
            };

            await _dynamoDbClient.UpdateItemAsync(updateRequest);

            _logger.LogInformation("Updated pet {PetId} with main image file extension {Extension}", petId, extension);

            // Get the pet to find its shelter ID for cache invalidation
            var petResponse = await GetPetById(petId);
            if (petResponse.Success && petResponse.Pet != null)
            {
                InvalidatePetCountCache(petResponse.Pet.ShelterId);
                InvalidatePetStatisticsCache(petResponse.Pet.ShelterId);
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
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Pet not found when generating upload URL: {PetId}", petId);
            return new PresignedUrlResponse
            {
                Success = false,
                ErrorMessage = "Pet not found"
            };
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
}
