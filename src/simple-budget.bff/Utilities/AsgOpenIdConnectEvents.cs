using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace simple_budget.bff.Utilities;

public static class AsgOpenIdConnectEvents
{
    public static Task OnTokenValidated(TokenValidatedContext ctx)
    {
        return Task.CompletedTask;
    }

    public static Task OnRedirectToIdentityProvider(RedirectContext context)
    {
        return Task.CompletedTask;
    }

    internal static Task OnTokenResponseReceived(TokenResponseReceivedContext context)
    {
        return Task.CompletedTask;
    }
}
