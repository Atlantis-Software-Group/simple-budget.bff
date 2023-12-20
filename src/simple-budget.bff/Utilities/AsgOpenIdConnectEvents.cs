using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace simple_budget.bff.Utilities;

public static class AsgOpenIdConnectEvents
{
    public static Task OnTokenValidated(TokenValidatedContext ctx)
    {
        return Task.CompletedTask;
    }
}
