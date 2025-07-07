using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Longhl104.PawfectMatch.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds AWS Systems Manager Parameter Store configuration for PawfectMatch services
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="environmentName">The environment name (Development, Staging, Production)</param>
    /// <param name="serviceName">The service name (Identity, ShelterHub, Matcher, etc.)</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddPawfectMatchSystemsManager(
        this WebApplicationBuilder builder,
        string serviceName
        )
    {

        builder.Configuration.AddSystemsManager(
            $"/PawfectMatch/{builder.Environment.EnvironmentName}"
        );

        builder.Configuration.AddSystemsManager($"/PawfectMatch/{builder.Environment.EnvironmentName}/{serviceName}");

        return builder.Configuration;
    }

    /// <summary>
    /// Adds AWS Systems Manager Parameter Store configuration for PawfectMatch services using a custom path
    /// </summary>
    /// <param name="configuration">The configuration builder</param>
    /// <param name="parameterPath">The full parameter path in Systems Manager</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddPawfectMatchSystemsManager(
        this IConfigurationBuilder configuration,
        string parameterPath
        )
    {
        return configuration.AddSystemsManager(parameterPath);
    }
}
