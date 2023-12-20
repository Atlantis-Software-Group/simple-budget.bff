using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace simple_budget.bff;

public class IdpRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdpRedirectOptions _options;

    public IdpRedirectMiddleware(RequestDelegate next, IOptions<IdpRedirectOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        
        if ( context.Response.StatusCode == (int)HttpStatusCode.Redirect 
                && !string.IsNullOrWhiteSpace(_options.RedirectFrom)
                && !string.IsNullOrWhiteSpace(_options.RedirectTo))
        {
            string location = context.Response.Headers.Location[0]!; 
            if ( location.StartsWith(_options.RedirectFrom) )
            {
                location = location.Replace(_options.RedirectFrom, _options.RedirectTo);
                context.Response.Headers.Location = new Microsoft.Extensions.Primitives.StringValues(location);
            } 
        }
    }
}
