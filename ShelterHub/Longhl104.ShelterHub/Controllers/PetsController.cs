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
    [HttpGet("shelter/{shelterId:guid}")]
    public async Task<ActionResult<GetPetsResponse>> GetPetsByShelterId(Guid shelterId)
    {
        var response = await petService.GetPetsByShelterId(shelterId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets paginated pets for a specific shelter
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100)</param>
    /// <param name="nextToken">Token for pagination</param>
    /// <returns>Paginated list of pets</returns>
    [HttpGet("shelter/{shelterId:guid}/paginated")]
    public async Task<ActionResult<GetPaginatedPetsResponse>> GetPaginatedPetsByShelterId(
        Guid shelterId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nextToken = null)
    {
        var request = new GetPetsRequest
        {
            PageSize = pageSize,
            NextToken = nextToken
        };

        var response = await petService.GetPaginatedPetsByShelterId(shelterId, request);

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
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PetResponse>> GetPetById(Guid id)
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
    [HttpPost("shelter/{shelterId:guid}")]
    public async Task<ActionResult<PetResponse>> CreatePet([FromBody] CreatePetRequest request, Guid shelterId)
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

        return CreatedAtAction(nameof(GetPetById), new { id = response.Pet!.PetId }, response);
    }

    /// <summary>
    /// Updates a pet's status
    /// </summary>
    /// <param name="id">The pet ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated pet</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<PetResponse>> UpdatePetStatus(Guid id, [FromBody] PetStatus status)
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
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<PetResponse>> DeletePet(Guid id)
    {
        var response = await petService.DeletePet(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Generates a presigned URL for uploading pet images
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="fileName">The name of the file to upload</param>
    /// <param name="contentType">The content type of the file</param>
    /// <returns>Presigned URL for upload</returns>
    [HttpPost("{petId:guid}/upload-url")]
    public async Task<ActionResult<PresignedUrlResponse>> GenerateUploadUrl(
        Guid petId,
        [FromQuery] string fileName,
        [FromQuery] string contentType,
        [FromQuery] long fileSizeBytes
        )
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return BadRequest(new { error = "Content type is required" });
        }

        var response = await petService.GenerateUploadUrl(petId, fileName, contentType, fileSizeBytes);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
