namespace simple_budget.bff;

public static class IdpRedirectExtensions
{
    public static IApplicationBuilder UseIdpRedirect(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<IdpRedirectMiddleware>();
    }

}
