namespace simple_budget.bff;

public interface ITokenManagementService
{
    bool IsAccessTokenValid(string accessToken);

    Task<string> GetAccessTokenAsync();
    Task<bool> RefreshTokenAsync();
}
