using Microsoft.AspNetCore.Mvc;
using Longhl104.ShelterHub.Models;
using Longhl104.ShelterHub.Services;

namespace Longhl104.ShelterHub.Controllers;

/// <summary>
/// Controller for managing pets in the shelter
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PetsController(IPetService petService) : ControllerBase
{
    /// <summary>
    /// Gets all pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>List of pets</returns>
    [HttpGet("shelter/{shelterId}")]
    public async Task<ActionResult<GetPetsResponse>> GetPetsByShelterId(string shelterId)
    {
        var response = await petService.GetPetsByShelterId(shelterId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific pet by ID
    /// </summary>
    /// <param name="id">The pet ID</param>
    /// <returns>Pet details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PetResponse>> GetPetById(string id)
    {
        var response = await petService.GetPetById(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Creates a new pet
    /// </summary>
    /// <param name="request">Pet creation request</param>
    /// <param name="shelterId">The shelter ID that owns the pet</param>
    /// <returns>Created pet</returns>
    [HttpPost("shelter/{shelterId}")]
    public async Task<ActionResult<PetResponse>> CreatePet([FromBody] CreatePetRequest request, string shelterId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await petService.CreatePet(request, shelterId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetPetById), new { id = response.Pet!.Id }, response);
    }

    /// <summary>
    /// Updates a pet's status
    /// </summary>
    /// <param name="id">The pet ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated pet</returns>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<PetResponse>> UpdatePetStatus(string id, [FromBody] PetStatus status)
    {
        var response = await petService.UpdatePetStatus(id, status);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes a pet
    /// </summary>
    /// <param name="id">The pet ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<PetResponse>> DeletePet(string id)
    {
        var response = await petService.DeletePet(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}
