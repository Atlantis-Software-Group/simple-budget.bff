using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff.Utilities;

public static class AsgJwtBearerEvents
{
    public static Task OnMessageReceived(MessageReceivedContext context)
    {
        string? sub = context.HttpContext.Request.Cookies["sub"];
        if ( string.IsNullOrWhiteSpace(sub) )
            return Task.CompletedTask;

        IServiceScopeFactory scopeFactory = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IMemoryCache memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            UserAuthenticationInformation? userInfo = memoryCache.Get<UserAuthenticationInformation>(sub);

            if ( userInfo is null )
                return Task.CompletedTask;
            
            List<AuthenticationToken> tokens = [
                new AuthenticationToken{ Name = "access_token", Value = userInfo.AccessToken ?? string.Empty}
            ];

            context.Properties.StoreTokens(tokens);

            ClaimsIdentity identity = new ClaimsIdentity(context.Scheme.ToString());

            context.Principal = new ClaimsPrincipal(identity);

            context.Success();
        }

        return Task.CompletedTask;
    }
}
