using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Longhl104.ShelterHub.Models;

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
    Task<GetPetsResponse> GetPetsByShelterId(string shelterId);

    /// <summary>
    /// Gets a specific pet by ID
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet details</returns>
    Task<PetResponse> GetPetById(string petId);

    /// <summary>
    /// Creates a new pet
    /// </summary>
    /// <param name="request">Pet creation request</param>
    /// <param name="shelterId">The shelter ID that owns the pet</param>
    /// <returns>Created pet</returns>
    Task<PetResponse> CreatePet(CreatePetRequest request, string shelterId);

    /// <summary>
    /// Updates a pet's status
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated pet</returns>
    Task<PetResponse> UpdatePetStatus(string petId, PetStatus status);

    /// <summary>
    /// Deletes a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Success status</returns>
    Task<PetResponse> DeletePet(string petId);
}

/// <summary>
/// Service for managing pet operations with DynamoDB
/// </summary>
public class PetService : IPetService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PetService> _logger;
    private readonly string _tableName;

    public PetService(
        IAmazonDynamoDB dynamoDbClient,
        IHostEnvironment environment,
        ILogger<PetService> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _environment = environment;
        _logger = logger;
        _tableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pets";
    }

    /// <summary>
    /// Gets all pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>List of pets</returns>
    public async Task<GetPetsResponse> GetPetsByShelterId(string shelterId)
    {
        try
        {
            _logger.LogInformation("Getting pets for shelter ID: {ShelterId}", shelterId);

            var query = new QueryRequest
            {
                TableName = _tableName,
                IndexName = "ShelterId-Index", // GSI for querying by shelter ID
                KeyConditionExpression = "ShelterId = :shelterId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":shelterId", new AttributeValue { S = shelterId } }
                }
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
    /// Gets a specific pet by ID
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet details</returns>
    public async Task<PetResponse> GetPetById(string petId)
    {
        try
        {
            _logger.LogInformation("Getting pet by ID: {PetId}", petId);

            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = petId } }
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
    public async Task<PetResponse> CreatePet(CreatePetRequest request, string shelterId)
    {
        try
        {
            _logger.LogInformation("Creating new pet for shelter {ShelterId}", shelterId);

            var pet = new Pet
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Species = request.Species,
                Breed = request.Breed,
                Age = request.Age,
                Gender = request.Gender,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                ShelterId = shelterId,
                Status = PetStatus.Available,
                DateAdded = DateTime.UtcNow
            };

            var putRequest = new PutItemRequest
            {
                TableName = _tableName,
                Item = ConvertPetToDynamoDbItem(pet)
            };

            await _dynamoDbClient.PutItemAsync(putRequest);

            _logger.LogInformation("Successfully created pet with ID: {PetId}", pet.Id);

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
    public async Task<PetResponse> UpdatePetStatus(string petId, PetStatus status)
    {
        try
        {
            _logger.LogInformation("Updating pet status for ID: {PetId} to {Status}", petId, status);

            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = petId } }
                },
                UpdateExpression = "SET #status = :status",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "Status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":status", new AttributeValue { S = status.ToString() } }
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
    public async Task<PetResponse> DeletePet(string petId)
    {
        try
        {
            _logger.LogInformation("Deleting pet with ID: {PetId}", petId);

            var deleteRequest = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = petId } }
                },
                ReturnValues = ReturnValue.ALL_OLD
            };

            var response = await _dynamoDbClient.DeleteItemAsync(deleteRequest);

            if (!response.Attributes.Any())
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
            Id = item["Id"].S,
            Name = item["Name"].S,
            Species = item["Species"].S,
            Breed = item["Breed"].S,
            Age = int.Parse(item["Age"].N),
            Gender = item["Gender"].S,
            Description = item["Description"].S,
            ImageUrl = item.ContainsKey("ImageUrl") ? item["ImageUrl"].S : null,
            ShelterId = item["ShelterId"].S,
            Status = Enum.Parse<PetStatus>(item["Status"].S),
            DateAdded = DateTime.Parse(item["DateAdded"].S)
        };
    }

    /// <summary>
    /// Converts a Pet object to a DynamoDB item
    /// </summary>
    private static Dictionary<string, AttributeValue> ConvertPetToDynamoDbItem(Pet pet)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "Id", new AttributeValue { S = pet.Id } },
            { "Name", new AttributeValue { S = pet.Name } },
            { "Species", new AttributeValue { S = pet.Species } },
            { "Breed", new AttributeValue { S = pet.Breed } },
            { "Age", new AttributeValue { N = pet.Age.ToString() } },
            { "Gender", new AttributeValue { S = pet.Gender } },
            { "Description", new AttributeValue { S = pet.Description } },
            { "ShelterId", new AttributeValue { S = pet.ShelterId } },
            { "Status", new AttributeValue { S = pet.Status.ToString() } },
            { "DateAdded", new AttributeValue { S = pet.DateAdded.ToString("O") } }
        };

        if (!string.IsNullOrEmpty(pet.ImageUrl))
        {
            item["ImageUrl"] = new AttributeValue { S = pet.ImageUrl };
        }

        return item;
    }
}
