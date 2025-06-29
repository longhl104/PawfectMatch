using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using Longhl104.PawfectMatch.Models.Identity;

namespace Login.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestLoginFunctionHandler_InvalidMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/identity/users/login"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(405, response.StatusCode);
    }

    [Fact]
    public async Task TestLoginFunctionHandler_EmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/identity/users/login",
            Body = ""
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task TestLoginFunctionHandler_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/identity/users/login",
            Body = "invalid json"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task TestLoginFunctionHandler_MissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var loginRequest = new LoginRequest
        {
            Email = "",
            Password = "testPassword123!"
        };
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/identity/users/login",
            Body = JsonSerializer.Serialize(loginRequest)
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(400, response.StatusCode);
        var responseBody = JsonSerializer.Deserialize<ApiResponse<object>>(response.Body);
        Assert.NotNull(responseBody);
        Assert.False(responseBody.Success);
        Assert.Contains("Email is required", responseBody.Message);
    }

    [Fact]
    public async Task TestLoginFunctionHandler_InvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var loginRequest = new LoginRequest
        {
            Email = "invalid-email",
            Password = "testPassword123!"
        };
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/identity/users/login",
            Body = JsonSerializer.Serialize(loginRequest)
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(400, response.StatusCode);
        var responseBody = JsonSerializer.Deserialize<ApiResponse<object>>(response.Body);
        Assert.NotNull(responseBody);
        Assert.False(responseBody.Success);
        Assert.Contains("Invalid email format", responseBody.Message);
    }

    [Fact]
    public async Task TestLoginFunctionHandler_ShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "short"
        };
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/identity/users/login",
            Body = JsonSerializer.Serialize(loginRequest)
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(400, response.StatusCode);
        var responseBody = JsonSerializer.Deserialize<ApiResponse<object>>(response.Body);
        Assert.NotNull(responseBody);
        Assert.False(responseBody.Success);
        Assert.Contains("Password must be at least 8 characters long", responseBody.Message);
    }
}
