using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff.Controllers
{
    [Route("[controller]/[action]")]    
    public class UserController : ControllerBase
    {
        public ICookieService CookieService { get; }
        public IMemoryCache MemoryCache { get; }
        public ITokenManagementService TokenManagementService { get; }

        public UserController(ICookieService cookieService, IMemoryCache memoryCache, ITokenManagementService tokenManagementService)
        {
            CookieService = cookieService;
            MemoryCache = memoryCache;
            TokenManagementService = tokenManagementService;
        }
        [HttpGet]
        public async Task<ActionResult> Login()
        {   
            bool challenge = !HttpContext.User.Identity?.IsAuthenticated ?? true;            

            if ( challenge )
                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);

            return RedirectToAction("LoggedIn");
        }

        [HttpGet]
        public Task<string> Logout()
        {
            return Task.FromResult("Logout");
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public Task LoggedIn()
        {            
            return Task.CompletedTask;
        }


        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public Task<UserAuthenticationInformation> Tokens()
        {
            string sub = CookieService.GetHeaderCookieValue(CookieContants.CookieName);
            MemoryCache.TryGetValue(sub, out UserAuthenticationInformation? userInfo);
            return Task.FromResult(userInfo ?? new UserAuthenticationInformation());
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<List<UserAuthenticationInformation>> RefreshTokens()
        {
            List<UserAuthenticationInformation> UserInformations = new();

            string sub = CookieService.GetHeaderCookieValue(CookieContants.CookieName);
            MemoryCache.TryGetValue(sub, out UserAuthenticationInformation? userInfo);

            UserInformations.Add(userInfo ?? new UserAuthenticationInformation());

            bool successful = await TokenManagementService.RefreshTokenAsync();
            if ( successful )
            {                
                MemoryCache.TryGetValue(sub, out UserAuthenticationInformation? updatedUserInfo);                   
                UserInformations.Add(updatedUserInfo ?? new UserAuthenticationInformation());
            }

            return UserInformations;
        }
    }
}
