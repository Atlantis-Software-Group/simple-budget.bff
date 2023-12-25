using System.IdentityModel.Tokens.Jwt;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff;

public class TokenManagementService : ITokenManagementService
{
    private readonly ICacheService _cacheService;
    private readonly ICookieService _cookieService;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configurationManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenManagementService(ICacheService cacheService, 
                                    ICookieService cookieService, 
                                    TimeProvider timeProvider, 
                                    IConfiguration configurationManager,
                                    IHttpClientFactory httpClientFactory)
    {
        _cacheService = cacheService;
        _cookieService = cookieService;
        _timeProvider = timeProvider;
        _configurationManager = configurationManager;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if ( string.IsNullOrWhiteSpace(Sub) )
            return string.Empty; 

        string accessToken = UserInfo is null ? string.Empty : UserInfo.AccessToken!;

        if ( string.IsNullOrWhiteSpace(accessToken) )
            return string.Empty;

        bool isTokenValid = IsAccessTokenValid(accessToken);

        if ( !isTokenValid )
        {
            accessToken = string.Empty;
            bool tokenRefreshed = await RefreshTokenAsync();            

            if ( tokenRefreshed )
            {
                accessToken = UserInfo?.AccessToken ?? string.Empty;
            }
        }

        return accessToken;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        string?  authority = _configurationManager["Authentication:OIDC:Authority"];
        string? clientID = _configurationManager["Authentication:OIDC:ClientID"];
        string? clientSecret = _configurationManager["Authentication:OIDC:ClientSecret"];

        if ( string.IsNullOrEmpty(authority) )
            return false;

        if ( string.IsNullOrEmpty(clientID) )
            return false;

        if ( string.IsNullOrEmpty(clientSecret) )
            return false;

        if ( string.IsNullOrWhiteSpace(UserInfo?.RefreshToken ?? string.Empty) )
            return false;

        HttpClient client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(authority);
        TokenResponse response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest{
            Address = "/connect/token",
            RefreshToken = UserInfo!.RefreshToken!,
            ClientId = clientID,
            ClientSecret = clientSecret
        });

        if ( response.IsError )
            return false;

        UserAuthenticationInformation userInfo = new UserAuthenticationInformation
        {
            AccessToken = response.AccessToken,
            IdToken = response.IdentityToken,
            RefreshToken = response.RefreshToken
        };

        _ = _cacheService.SetEntry(Sub, userInfo);

        return true;
    }

    public bool IsAccessTokenValid(string accessToken)
    {
        JwtSecurityToken? jwt = null;

        try
        {
            jwt = new JwtSecurityToken(accessToken);
        }
        catch (Exception e) {
            string message = e.Message;
        }

        if ( jwt is null )
            return false;

        string tokenExp = jwt.Claims.First(claim => claim.Type.Equals("exp")).Value;
        long ticks = long.Parse(tokenExp);

        // consider the token expired 30 seconds before expirations
        // this should allow for refresh token logic. 
        DateTime tokenExpirationDate = DateTimeOffset.FromUnixTimeSeconds(ticks).UtcDateTime.AddSeconds(-30);
        //DateTime now = DateTime.Now.ToUniversalTime();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        return tokenExpirationDate >= utcNow;
    }

    protected UserAuthenticationInformation? UserInfo 
    {
        get {
            UserAuthenticationInformation? userInfo = _cacheService.GetEntry<UserAuthenticationInformation>(Sub);
            
            return userInfo;
        }
    }

    protected string Sub {
        get {
            if ( string.IsNullOrWhiteSpace(_subClaim) )
            {
                try
                {
                    Sub = _cookieService.GetHeaderCookieValue(CookieContants.CookieName);
                }
                catch (Exception) {}
            }
            return _subClaim;
        }
        set {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(value);
            _subClaim = value;
        }
    }

    private string _subClaim = string.Empty;
}
