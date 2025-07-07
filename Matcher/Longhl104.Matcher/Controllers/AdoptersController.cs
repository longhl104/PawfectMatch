using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest("UserId and FullName are required");
        }

        // Create the adopter in DynamoDB
        var putItemRequest = new PutItemRequest
        {
            TableName = _adoptersTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "UserId", new AttributeValue { S = request.UserId } },
                { "FullName", new AttributeValue { S = request.FullName } },
            }
        };

        await dynamoDbClient.PutItemAsync(putItemRequest);
        return Ok();
    }
}

public class CreateAdopterRequest
{
    public required string UserId { get; set; }
    public required string FullName { get; set; }
}

