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
    /// <param name="status">Filter by pet status (optional - null means all statuses)</param>
    /// <param name="species">Filter by pet species (optional - null means all species)</param>
    /// <param name="name">Search by pet name (optional - case-insensitive partial match)</param>
    /// <param name="breed">Search by pet breed (optional - case-insensitive partial match)</param>
    /// <returns>Paginated list of pets</returns>
    [HttpGet("shelter/{shelterId:guid}/paginated")]
    public async Task<ActionResult<GetPaginatedPetsResponse>> GetPaginatedPetsByShelterId(
        Guid shelterId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nextToken = null,
        [FromQuery] PetStatus? status = null,
        [FromQuery] string? species = null,
        [FromQuery] string? name = null,
        [FromQuery] string? breed = null)
    {
        var request = new GetPetsRequest
        {
            PageSize = pageSize,
            NextToken = nextToken,
            Status = status,
            Species = species,
            Name = name,
            Breed = breed
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
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status201Created)]
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
    /// Updates an existing pet
    /// </summary>
    /// <param name="id">The pet ID</param>
    /// <param name="request">Pet update request</param>
    /// <returns>Updated pet</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PetResponse>> UpdatePet(Guid id, [FromBody] UpdatePetRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await petService.UpdatePet(id, request);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
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

    /// <summary>
    /// Gets download presigned URLs for pet images
    /// </summary>
    /// <param name="request">Request containing pet IDs and their file extensions</param>
    /// <returns>Dictionary of pet IDs to their download presigned URLs</returns>
    [HttpPost("images/download-urls")]
    public async Task<ActionResult<PetImageDownloadUrlsResponse>> GetPetImageDownloadUrls(
        [FromBody] GetPetImageDownloadUrlsRequest request)
    {
        if (request == null || request.PetRequests == null || request.PetRequests.Count == 0)
        {
            return BadRequest(new { error = "Request must contain at least one pet request" });
        }

        var response = await petService.GetPetImageDownloadUrls(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
