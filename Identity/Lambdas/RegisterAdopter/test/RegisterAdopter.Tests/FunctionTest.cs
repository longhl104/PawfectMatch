using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

namespace RegisterAdopter.Tests;

public class FunctionTest
{
    // [Fact]
    // public async Task TestToUpperFunction()
    // {
    //     // Set required environment variable
    //     Environment.SetEnvironmentVariable("USER_POOL_ID", "ap-southeast-2_cZ2gz0MNu");

    //     // Invoke the lambda function and confirm the string was upper cased.
    //     var function = new Function();
    //     var context = new TestLambdaContext();
    //     var request = new APIGatewayProxyRequest
    //     {
    //         Body = "hello world"
    //     };

    //     var response = await function.FunctionHandler(request, context);

    //     Assert.Equal("HELLO WORLD", response.Body);
    // }
}
