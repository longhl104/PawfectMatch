using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Longhl104.Identity.Configuration;

public static class ParameterStoreConfigurationExtensions
{
    public static IConfigurationBuilder AddParameterStore(
        this IConfigurationBuilder builder,
        string parameterPrefix,
        bool optional = true)
    {
        return builder.Add(new ParameterStoreConfigurationSource(parameterPrefix, optional));
    }
}

public class ParameterStoreConfigurationSource(string parameterPrefix, bool optional) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ParameterStoreConfigurationProvider(parameterPrefix, optional);
    }
}

public class ParameterStoreConfigurationProvider(string parameterPrefix, bool optional) : ConfigurationProvider
{
    private readonly AmazonSimpleSystemsManagementClient _ssmClient = new();

    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (!optional)
            {
                throw new InvalidOperationException($"Failed to load parameters from Parameter Store with prefix '{parameterPrefix}'", ex);
            }

            // Log the error but continue if optional
            Console.WriteLine($"Warning: Failed to load optional parameters from Parameter Store: {ex.Message}");
        }
    }

    private async Task LoadAsync()
    {
        var request = new GetParametersByPathRequest
        {
            Path = parameterPrefix,
            Recursive = true,
            WithDecryption = true
        };

        var parameters = new Dictionary<string, string?>();

        do
        {
            var response = await _ssmClient.GetParametersByPathAsync(request);

            foreach (var parameter in response.Parameters)
            {
                // Remove the prefix and convert to configuration key format
                var key = parameter.Name[parameterPrefix.Length..].TrimStart('/');

                // Convert AWS parameter hierarchy to configuration format
                // e.g., /app/jwt/key becomes JWT:Key
                key = key.Replace('/', ':');

                parameters[key] = parameter.Value;
            }

            request.NextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(request.NextToken));

        Data = parameters;
    }
}
