using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.Matcher.Models;

namespace Longhl104.Matcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdoptersController(
    IHostEnvironment hostEnvironment,
    IAmazonDynamoDB dynamoDbClient
) : ControllerBase
{
    private readonly string _adoptersTableName = $"pawfectmatch-{hostEnvironment.EnvironmentName.ToLowerInvariant()}-matcher-adopters";

    [HttpPost()]
    [Authorize(Policy = "InternalOnly")]
    [Route("~/api/internal/[controller]")]
    public async Task<IActionResult> CreateAdopter([FromBody] CreateAdopterRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.UserId) ||
            (string.IsNullOrWhiteSpace(request.FirstName) && string.IsNullOrWhiteSpace(request.LastName)))
        {
            return BadRequest("UserId and at least FirstName or LastName are required");
        }

        // Create the adopter in DynamoDB
        var putItemRequest = new PutItemRequest
        {
            TableName = _adoptersTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "UserId", new AttributeValue { S = request.UserId } },
                { "FirstName", new AttributeValue { S = request.FirstName?.Trim() ?? string.Empty } },
                { "LastName", new AttributeValue { S = request.LastName?.Trim() ?? string.Empty } }
            }
        };

        await dynamoDbClient.PutItemAsync(putItemRequest);
        return Ok();
    }
}

