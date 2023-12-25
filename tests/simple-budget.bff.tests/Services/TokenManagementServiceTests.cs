using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NuGet.Frameworks;
using simple_budget.bff.Models;
using Xunit.Abstractions;

namespace simple_budget.bff.tests;

public class TokenManagementServiceTests
{
    public TokenManagementService TokenManagementService { get; set; }
    public ITestOutputHelper TestOutputHelper { get; }

    private readonly ICacheService memoryCache;
    private readonly ICookieService cookieService;
    private readonly FakeTimeProvider timeProvider;
    private readonly IConfigurationManager configurationManager;
    private readonly IHttpClientFactory httpClientFactory;

    public TokenManagementServiceTests(ITestOutputHelper testOutputHelper)
    {
        memoryCache = Substitute.For<ICacheService>();
        cookieService = Substitute.For<ICookieService>();
        timeProvider = new FakeTimeProvider();
        configurationManager = Substitute.For<IConfigurationManager>();
        httpClientFactory = Substitute.For<IHttpClientFactory>();

        TokenManagementService = new TokenManagementService(memoryCache, cookieService, timeProvider, configurationManager, httpClientFactory);
        TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RefreshToken_False_Authority_Setting_NotFound()
    {
        configurationManager["Authentication:OIDC:Authority"].Returns(string.Empty);

        bool result = await TokenManagementService.RefreshTokenAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshToken_False_RefreshToken_NotAvailabe()
    {
        configurationManager["Authentication:OIDC:Authority"].Returns("https://www.random.com");
        cookieService.GetHeaderCookieValue(CookieContants.CookieName).Returns("Hi,I am the Cookie Monster");
        memoryCache.GetEntry<UserAuthenticationInformation>(Arg.Any<string>()).Returns(new UserAuthenticationInformation());
        
        bool result = await TokenManagementService.RefreshTokenAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshToken_False_UserInfo_NotFoundInCache()
    {
        configurationManager["Authentication:OIDC:Authority"].Returns("https://www.random.com");
        cookieService.GetHeaderCookieValue(CookieContants.CookieName).Returns("Hi,I am the Cookie Monster");
        memoryCache.GetEntry<UserAuthenticationInformation>(Arg.Any<string>()).Returns(default(UserAuthenticationInformation));

        bool result = await TokenManagementService.RefreshTokenAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshToken_False_Authority_NotFound()
    {

        configurationManager["Authentication:OIDC:Authority"].Returns("https://www.random.com");
        cookieService.GetHeaderCookieValue(CookieContants.CookieName).Returns("Hi,I am the Cookie Monster");
        memoryCache.GetEntry<UserAuthenticationInformation>(Arg.Any<string>()).Returns(new UserAuthenticationInformation(){
            RefreshToken = "Hello, I am the Cookie Monster. Please refresh my token"
        });

        HttpClient httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.NotFound, string.Empty));
        httpClientFactory.CreateClient().Returns(httpClient);

        bool result = await TokenManagementService.RefreshTokenAsync();

        Assert.False(result);

        // Need to check what information was passed into the cache. Need to Create a ICache interface and implementation. 
        // this implementation is a wrapper around IMemoryCache.   
    }

    [Fact]
    public async Task RefreshToken_True_TokensUpdated()
    {

        UserAuthenticationInformation userInfo = new UserAuthenticationInformation(){
            RefreshToken = "Hello, I am the Cookie Monster. Please refresh my token"
        };
        configurationManager["Authentication:OIDC:Authority"].Returns("https://www.random.com");
        configurationManager["Authentication:OIDC:ClientID"].Returns("client");
        configurationManager["Authentication:OIDC:ClientSecret"].Returns("clientSecret");
        cookieService.GetHeaderCookieValue(CookieContants.CookieName).Returns("Hi,I am the Cookie Monster");
        memoryCache.GetEntry<UserAuthenticationInformation>(Arg.Any<string>()).Returns(userInfo);

        HttpClient httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, jsonTokenResponse));
        httpClientFactory.CreateClient().Returns(httpClient);

        UserAuthenticationInformation? updatedUserInfo = null;
        memoryCache.SetEntry<UserAuthenticationInformation>(Arg.Any<string>(), Arg.Do<UserAuthenticationInformation>(x => updatedUserInfo = x));
                    

        bool result = await TokenManagementService.RefreshTokenAsync();

        Assert.True(result);

        Assert.False(string.IsNullOrWhiteSpace(updatedUserInfo?.AccessToken ?? string.Empty));
        Assert.False(string.IsNullOrWhiteSpace(updatedUserInfo?.IdToken ?? string.Empty));
        Assert.False(string.IsNullOrWhiteSpace(updatedUserInfo?.RefreshToken ?? string.Empty));
        Assert.NotEqual("Hello, I am the Cookie Monster. Please refresh my token", updatedUserInfo?.RefreshToken ?? string.Empty); 
    }

    [Fact]
    public void IsAccessTokenValid_False_Malformed()
    {
        string accessToken = "randomSomething";

        bool result = TokenManagementService.IsAccessTokenValid(accessToken);

        Assert.False(result);
    }

    [Fact]
    public void IsAccessTokenValid_False_AlmostExpired()
    {
        DateTimeOffset utcNow = DateTimeOffset.FromUnixTimeSeconds(expClaim).UtcDateTime.AddSeconds(-25);
        timeProvider.SetUtcNow(utcNow);

        bool result = TokenManagementService.IsAccessTokenValid(access_token);

        Assert.False(result);
    }

    [Fact]
    public void IsAccessTokenValid_True()
    {
        DateTimeOffset utcNow = DateTimeOffset.FromUnixTimeSeconds(expClaim).UtcDateTime.AddMinutes(-25);
        timeProvider.SetUtcNow(utcNow);

        bool result = TokenManagementService.IsAccessTokenValid(access_token);

        Assert.True(result);
    }

    private string jsonTokenResponse = @"{
        ""id_token"": ""eyJhbGciOiJSUzI1NiIsImtpZCI6IkJERDlCMERGODVBNkNDNjM4MjJBRENEOUQwM0ZGQ0NFIiwidHlwIjoiSldUIn0.eyJpc3MiOiJodHRwczovL2xvY2FsaG9zdDozMTAzIiwibmJmIjoxNzAzNDgwNjA4LCJpYXQiOjE3MDM0ODA2MDgsImV4cCI6MTcwMzQ4MDkwOCwiYXVkIjoiYmFja2VuZC1mb3ItZnJvbnRlbmQiLCJhbXIiOlsicHdkIl0sImF0X2hhc2giOiJTQnZUZnZFX0dyS0Z4VnRJNW0xbTBnIiwic2lkIjoiMjRFRjI3MTBDMkQ1MTFDNkNCQ0NCQUQ4MTE2NkU3OUQiLCJzdWIiOiIyOTgxNTdiMi05ODA5LTRiZDMtOWQ5Ni0zODljNzk0Nzg2NGUiLCJhdXRoX3RpbWUiOjE3MDM0Nzk4MDYsImlkcCI6ImxvY2FsIn0.ps5mahfJYQpvxNV07z5gSZNyk-xW_DRr5RaDsDtV3gcoHfAmkDG3vAiRh7fgWhEB4G_hKcJ-uDRD1P936SCe8ynS4Mbg7-Baw7Lr_WutQn-5ajReVR4-ie6B2mlBmwA-lojY2R-PHBLkb2jr7O3n5G5SxgLFbLeSl-v8r8p4YZCShtDstcpdGbhKQxgy-c_BgY2VokN-8w8b_7KF5uhQRtOBrOhcntDzdSny1q1VQr5f4boIYGy0QAZdXPN6cJZTDg-j5w6U9bgbNhyisEnkA4YvbH4rZ942I01uATcLSbGwp4mHvVZlWvFAItGaMJks_OnyEc6yzD_eJJAErV15ew"",
  ""access_token"": ""eyJhbGciOiJSUzI1NiIsImtpZCI6IkJERDlCMERGODVBNkNDNjM4MjJBRENEOUQwM0ZGQ0NFIiwidHlwIjoiYXQrand0In0.eyJpc3MiOiJodHRwczovL2lkZW50aXR5OjUwMDEiLCJuYmYiOjE3MDM0ODA2MDgsImlhdCI6MTcwMzQ4MDYwOCwiZXhwIjoxNzAzNDg0MjA4LCJzY29wZSI6WyJvcGVuaWQiLCJ1c2VyIiwiZW1haWwiLCJvZmZsaW5lX2FjY2VzcyJdLCJhbXIiOlsicHdkIl0sImNsaWVudF9pZCI6ImJhY2tlbmQtZm9yLWZyb250ZW5kIiwic3ViIjoiMjk4MTU3YjItOTgwOS00YmQzLTlkOTYtMzg5Yzc5NDc4NjRlIiwiYXV0aF90aW1lIjoxNzAzNDc5ODA2LCJpZHAiOiJsb2NhbCIsInNpZCI6IjI0RUYyNzEwQzJENTExQzZDQkNDQkFEODExNjZFNzlEIiwianRpIjoiNDk4Q0VGMkVBNzkyMERGOTExNDA1ODlEMjlCMEFBM0EifQ.15goPHPvIhFe5ulfGI8OYJc4aTA_3uCKovCsdMXQWkB9rRaNuqfm1zVuFFZnJ0AtwN7Jw6BpXXEkfUZCbLd8O0RCzqVjp36LP-T-dl4epOvOZJlbY3BB1x5qnOHiDMgD4k2-sYPjRnxtfdjn13Gl8zCCTcJnAGDxkwiXW3_kNw_9y-51B6FWIyj0j9HrGz9hqzDD3PHT5m0K5oOr5d07udE11XwxFP4TDxG5OPoh_6iywK5GgxmJU0kw2YnufGKLEyWEzjCtQJJYN4RP-iS3tec0Rpto4co6ymHbhrEDcXYNwCC5pXZ9j5s_oZfevptyNhslOMOKussSKi28dm6zPw"",
  ""expires_in"": 3600,
  ""token_type"": ""Bearer"",
  ""refresh_token"": ""DF467469E398B0AC8AA19047959ACFB6983381F0F56E7C46F2F6C9B831983BA2-1"",
  ""scope"": ""openid offline_access user email""}";

    /*
    * exp in the token is set to December 24, 2023 9:19:01 PM GMT-05:00
    * exp claim value = 63870689941
    */
    private long expClaim = 63870689941;
    private string access_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjYzODcwNjg5OTQxfQ.efFBHb0xMKQVAUw5Zc0dwKb6J2_es-8cPT0vE3aJIHw";
}
