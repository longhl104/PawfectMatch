using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Longhl104.ShelterHub.Models;
using Longhl104.PawfectMatch.Extensions;
using System.Globalization;

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
    private readonly string _tableName;
    private readonly string _bucketName;
    private readonly string _shelterIdCreatedAtIndex = "ShelterIdCreatedAtIndex";

    public PetService(
        IAmazonDynamoDB dynamoDbClient,
        IHostEnvironment environment,
        ILogger<PetService> logger,
        IMediaService mediaUploadService)
    {
        _dynamoDbClient = dynamoDbClient;
        _environment = environment;
        _logger = logger;
        _mediaUploadService = mediaUploadService;
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

            // Get total count with a separate query (including filters)
            var countQuery = new QueryRequest
            {
                TableName = _tableName,
                IndexName = _shelterIdCreatedAtIndex, // GSI for querying by shelter ID
                KeyConditionExpression = "ShelterId = :shelterId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":shelterId", new AttributeValue { S = shelterId.ToString() } }
                },
                Select = Select.COUNT
            };

            // Apply the same filters to the count query
            if (filterConditions.Count > 0)
            {
                countQuery.FilterExpression = query.FilterExpression;
                countQuery.ExpressionAttributeNames = query.ExpressionAttributeNames;

                // Add filter values to count query
                if (request.Status.HasValue)
                {
                    countQuery.ExpressionAttributeValues[":status"] = new AttributeValue { S = request.Status.Value.GetAmbientValue<string>() };
                }

                if (!string.IsNullOrWhiteSpace(request.Species))
                {
                    countQuery.ExpressionAttributeValues[":species"] = new AttributeValue { S = request.Species };
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    countQuery.ExpressionAttributeValues[":petName"] = new AttributeValue { S = request.Name };
                }

                if (!string.IsNullOrWhiteSpace(request.Breed))
                {
                    countQuery.ExpressionAttributeValues[":breed"] = new AttributeValue { S = request.Breed };
                }
            }

            var countResponse = await _dynamoDbClient.QueryAsync(countQuery);
            var totalCount = countResponse.Count ?? 0;

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
                Age = request.Age,
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
            Age = int.Parse(item["Age"].N),
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
            { "Age", new AttributeValue { N = pet.Age.ToString() } },
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
}
