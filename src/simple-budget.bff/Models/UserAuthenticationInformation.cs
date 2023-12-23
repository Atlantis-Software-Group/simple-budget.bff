namespace simple_budget.bff.Models;

public class UserAuthenticationInformation
{
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
}
