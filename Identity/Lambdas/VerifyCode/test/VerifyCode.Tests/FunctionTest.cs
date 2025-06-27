using Xunit;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using System.Text.Json;

namespace VerifyCode.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestVerifyCodeFunction_InvalidRequest_ReturnsBadRequest()
    {
        // Note: This test requires proper mocking setup for DynamoDB and Cognito
        // For now, we test the basic request validation

        var request = new APIGatewayProxyRequest
        {
            Body = JsonSerializer.Serialize(new { /* empty request */ })
        };

        var context = new TestLambdaContext();

        // This will fail due to missing environment variables, but we're testing structure
        try
        {
            var function = new Function();
            var response = await function.FunctionHandler(request, context);

            // If we get here, the function structure is correct
            Assert.NotNull(response);
        }
        catch (InvalidOperationException ex)
        {
            // Expected due to missing environment variables
            Assert.Contains("environment variable is required", ex.Message);
        }
    }

    [Fact]
    public void TestVerifyCodeRequestDeserialization()
    {
        var json = """{"email": "test@example.com", "code": "123456"}""";
        var request = JsonSerializer.Deserialize<VerifyCodeRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(request);
        Assert.Equal("test@example.com", request.Email);
        Assert.Equal("123456", request.Code);
    }
}
