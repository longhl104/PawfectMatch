using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Moq;
using Xunit;

namespace InstallPostgis.Tests;

public class FunctionTest
{
    [Fact]
    public void TestToUpperFunction()
    {
        // Invoke the lambda function and confirm the string was upper cased.
        var function = new Function();
        var context = new TestLambdaContext();

        // Note: For a real test, you would need to mock the SecretsManager and database connections
        // This is a basic structure for testing

        Assert.NotNull(function);
    }

    [Fact]
    public void TestPostgisInstallationRequest()
    {
        var request = new PostgisInstallationRequest
        {
            Stage = "development"
        };

        Assert.Equal("development", request.Stage);
    }

    [Fact]
    public void TestPostgisInstallationResponse()
    {
        var response = new PostgisInstallationResponse
        {
            Success = true,
            Message = "PostGIS extension installed successfully",
            ExtensionVersion = "3.4.0"
        };

        Assert.True(response.Success);
        Assert.Equal("PostGIS extension installed successfully", response.Message);
        Assert.Equal("3.4.0", response.ExtensionVersion);
    }

    [Fact]
    public void TestDatabaseSecret()
    {
        var secret = new DatabaseSecret
        {
            Engine = "postgres",
            Host = "localhost",
            Username = "dbadmin",
            Password = "password123",
            DbName = "pawfectmatch",
            Port = 5432
        };

        Assert.Equal("postgres", secret.Engine);
        Assert.Equal("localhost", secret.Host);
        Assert.Equal("dbadmin", secret.Username);
        Assert.Equal("password123", secret.Password);
        Assert.Equal("pawfectmatch", secret.DbName);
        Assert.Equal(5432, secret.Port);
    }
}
