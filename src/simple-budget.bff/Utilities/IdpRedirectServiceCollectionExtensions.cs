namespace simple_budget.bff;

public static class IdpRedirectServiceCollectionExtensions
{
    public static IServiceCollection AddIdpRedirect(this IServiceCollection services, Action<IdpRedirectOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }
        
}
