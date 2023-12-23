using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;

namespace simple_budget.bff;

public static class CookieEvents
{
    public static Task OnRedirectToLogin(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        ctx.Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    public static Task OnRedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        ctx.Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}