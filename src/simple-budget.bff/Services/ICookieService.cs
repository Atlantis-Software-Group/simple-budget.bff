namespace simple_budget.bff;

public interface ICookieService
{
    bool SetResponseCookie(string name, string value, CookieOptions? options = null);
    string GetHeaderCookieValue(string name);
}