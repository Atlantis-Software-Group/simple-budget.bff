using Microsoft.AspNetCore.DataProtection;

namespace simple_budget.bff;

public class CookieService : ICookieService
{
    private CookieOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _dataProtector;

    public CookieService(IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("SimpleBudget.Bff");
        _httpContextAccessor = httpContextAccessor;     
        _options = new CookieOptions{
            IsEssential = true,
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        };   
    }
    public bool SetResponseCookie(string name, string value, CookieOptions? options = null)
    {
        HttpContext? ctx = _httpContextAccessor.HttpContext;

        ArgumentNullException.ThrowIfNull(ctx);

        _options.Domain = ctx.Request.Host.Host;

        CookieOptions _opt = options is null ? _options : options;

        string cookieValue = _dataProtector.Protect(value);
        ctx.Response.Cookies.Append(name, cookieValue, _opt);

        return true;
    }

    public string GetHeaderCookieValue(string name)
    {
        HttpContext? ctx = _httpContextAccessor.HttpContext;

        ArgumentNullException.ThrowIfNull(ctx);

        string result = ctx.Request.Cookies[name] ?? string.Empty;

        if ( !string.IsNullOrWhiteSpace(result) )
            result = _dataProtector.Unprotect(result);

        return result;
    }
}
