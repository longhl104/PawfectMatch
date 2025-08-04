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
    /// Gets all available pet species
    /// </summary>
    /// <returns>List of all pet species</returns>
    [HttpGet("species")]
    public async Task<ActionResult<GetPetSpeciesResponse>> GetAllPetSpecies()
    {
        var response = await petService.GetAllPetSpecies();

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets all breeds for a specific species
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>List of breeds for the species</returns>
    [HttpGet("species/{speciesId:int}/breeds")]
    public async Task<ActionResult<GetPetBreedsResponse>> GetBreedsBySpeciesId(int speciesId)
    {
        var response = await petService.GetBreedsBySpeciesId(speciesId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

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

    /// <summary>
    /// Gets all media files (images, videos, documents) for a specific pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <returns>Pet media files organized by type</returns>
    [HttpGet("{petId:guid}/media")]
    public async Task<ActionResult<GetPetMediaResponse>> GetPetMedia(Guid petId)
    {
        var response = await petService.GetPetMedia(petId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Generates presigned URLs for uploading multiple media files for a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="request">Request containing media file details</param>
    /// <returns>Presigned URLs for uploading media files</returns>
    [HttpPost("{petId:guid}/media/upload-urls")]
    public async Task<ActionResult<MediaFileUploadResponse>> GenerateMediaUploadUrls(
        Guid petId,
        [FromBody] UploadMediaFilesRequest request)
    {
        if (request == null || request.MediaFiles == null || request.MediaFiles.Count == 0)
        {
            return BadRequest(new { error = "Request must contain at least one media file" });
        }

        // Ensure the pet ID in the request matches the route parameter
        request.PetId = petId;

        var response = await petService.GenerateMediaUploadUrls(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Confirms successful upload of media files and updates the database
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="mediaFileIds">List of media file IDs that were successfully uploaded</param>
    /// <returns>Success status</returns>
    [HttpPost("{petId:guid}/media/confirm-uploads")]
    public async Task<ActionResult<GetPetMediaResponse>> ConfirmMediaUploads(
        Guid petId,
        [FromBody] List<Guid> mediaFileIds
        )
    {
        if (mediaFileIds == null || mediaFileIds.Count == 0)
        {
            return BadRequest(new { error = "Media file IDs are required" });
        }

        var response = await petService.ConfirmMediaUploads(petId, mediaFileIds);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes multiple media files for a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="request">Request containing media file IDs to delete</param>
    /// <returns>Delete operation results</returns>
    [HttpDelete("{petId:guid}/media")]
    public async Task<ActionResult<DeleteMediaFilesResponse>> DeleteMediaFiles(
        Guid petId,
        [FromBody] DeleteMediaFilesRequest request)
    {
        if (request == null || request.MediaFileIds == null || request.MediaFileIds.Count == 0)
        {
            return BadRequest(new { error = "Media file IDs are required" });
        }

        var response = await petService.DeleteMediaFiles(petId, request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Updates the display order of media files for a pet
    /// </summary>
    /// <param name="petId">The pet ID</param>
    /// <param name="mediaFileOrders">Dictionary of media file ID to display order</param>
    /// <returns>Success status</returns>
    [HttpPut("{petId:guid}/media/reorder")]
    public async Task<ActionResult<GetPetMediaResponse>> ReorderMediaFiles(
        Guid petId,
        [FromBody] Dictionary<Guid, int> mediaFileOrders)
    {
        if (mediaFileOrders == null || mediaFileOrders.Count == 0)
        {
            return BadRequest(new { error = "Media file orders are required" });
        }

        var response = await petService.ReorderMediaFiles(petId, mediaFileOrders);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
