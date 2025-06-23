using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

namespace PostGisInstaller.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestToUpperFunction()
    {

        // Invoke the lambda function and confirm the string was upper cased.
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new CustomResourceRequest();
        var response = await function.FunctionHandler(request, context);

        Assert.Equal("SUCCESS", response.Status);
    }
}
