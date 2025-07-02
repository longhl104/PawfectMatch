namespace Longhl104.Identity.Configuration;

public static class ParameterStoreConfigurationExtensions
{
    public static IConfigurationBuilder AddParameterStore(
        this IConfigurationBuilder builder,
        string parameterPrefix
        )
    {
        return builder.AddSystemsManager(parameterPrefix);
    }
}
