using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff.Utilities;

public static class AsgJwtBearerEvents
{
    public static async Task OnMessageReceived(MessageReceivedContext context)
    {
        IServiceScopeFactory scopeFactory = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        using (AsyncServiceScope scope = scopeFactory.CreateAsyncScope())
        {            
            ITokenManagementService tokenManagementService = scope.ServiceProvider.GetRequiredService<ITokenManagementService>();
            string access_token = await tokenManagementService.GetAccessTokenAsync();

            if ( string.IsNullOrWhiteSpace(access_token) )
                return;

            List<AuthenticationToken> tokens = [
                new AuthenticationToken{ Name = "access_token", Value = access_token}
            ];

            context.Properties.StoreTokens(tokens);

            ClaimsIdentity identity = new ClaimsIdentity(context.Scheme.ToString());

            context.Principal = new ClaimsPrincipal(identity);

            context.Success();
        }

        return;
    }
}
