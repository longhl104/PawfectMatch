using Longhl104.ShelterHub.Models;

namespace Longhl104.ShelterHub.Services;

/// <summary>
/// Service for managing pet operations
/// </summary>
public class PetService
{
    // In a real implementation, this would use a database context
    private readonly List<Pet> _pets = new();

    /// <summary>
    /// Gets all pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>List of pets</returns>
    public async Task<GetPetsResponse> GetPetsByShelterId(string shelterId)
    {
        try
        {
            var pets = _pets.Where(p => p.ShelterId == shelterId).ToList();
            return new GetPetsResponse
            {
                Success = true,
                Pets = pets
            };
        }
        catch (Exception ex)
        {
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
            var pet = _pets.FirstOrDefault(p => p.Id == petId);
            if (pet == null)
            {
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            return new PetResponse
            {
                Success = true,
                Pet = pet
            };
        }
        catch (Exception ex)
        {
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

            _pets.Add(pet);

            return new PetResponse
            {
                Success = true,
                Pet = pet
            };
        }
        catch (Exception ex)
        {
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
            var pet = _pets.FirstOrDefault(p => p.Id == petId);
            if (pet == null)
            {
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            pet.Status = status;

            return new PetResponse
            {
                Success = true,
                Pet = pet
            };
        }
        catch (Exception ex)
        {
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
            var pet = _pets.FirstOrDefault(p => p.Id == petId);
            if (pet == null)
            {
                return new PetResponse
                {
                    Success = false,
                    ErrorMessage = "Pet not found"
                };
            }

            _pets.Remove(pet);

            return new PetResponse
            {
                Success = true,
                Pet = pet
            };
        }
        catch (Exception ex)
        {
            return new PetResponse
            {
                Success = false,
                ErrorMessage = $"Failed to delete pet: {ex.Message}"
            };
        }
    }
}
