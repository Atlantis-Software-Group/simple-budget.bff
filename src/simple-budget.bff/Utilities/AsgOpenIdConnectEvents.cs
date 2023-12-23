using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff.Utilities;

public static class AsgOpenIdConnectEvents
{
    public static Task OnTokenValidated(TokenValidatedContext ctx)
    {
        IDictionary<string, string> parameters = ctx.TokenEndpointResponse?.Parameters ?? new Dictionary<string, string>();
        if ( !parameters.Any() || ctx.Properties is null )
            return Task.CompletedTask;

        IServiceScopeFactory scopeFactory = ctx.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IMemoryCache cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            MemoryCacheEntryOptions opts = new MemoryCacheEntryOptions()
                                                .SetAbsoluteExpiration(TimeSpan.FromDays(5))
                                                .SetPriority(CacheItemPriority.Normal)
                                                .SetSize(1024);
            
            JwtSecurityToken jwt = new JwtSecurityToken(parameters["access_token"]);
            string cacheKey = jwt.Subject;

            UserAuthenticationInformation userInfo = new UserAuthenticationInformation{
                AccessToken = parameters["access_token"],
                IdToken = parameters["id_token"],
                RefreshToken = parameters["refresh_token"]
            };

            cache.GetOrCreate(cacheKey, (cacheEntry) => {
                return userInfo;
            });

            ctx.Response.Cookies.Append("sub", cacheKey, new CookieOptions{                
                IsEssential = true,
                Secure = true,
                HttpOnly = true,
                Domain = "localhost",
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddSeconds(3500)
            });
        }

        return Task.CompletedTask;
    }
}
