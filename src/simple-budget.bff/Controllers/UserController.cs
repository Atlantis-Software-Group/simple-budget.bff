using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using simple_budget.bff.Models;

namespace simple_budget.bff.Controllers
{
    [Route("[controller]")]    
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
        [HttpGet("login")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Login()
        {   
            bool challenge = !HttpContext.User.Identity?.IsAuthenticated ?? true;            

            if ( challenge )
                await HttpContext.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme);       

            return Redirect("redirecttospa");
        }

        [HttpGet("redirecttospa", Name = "RedirectToSPA")]
        public Task<RedirectResult> RedirectToSPA()
        {
            return Task.FromResult(Redirect("https://localhost:3100/"));
        }

        [HttpGet("logout")]
        public Task<string> Logout()
        {
            return Task.FromResult("Logout");
        }

        [HttpGet("loggedin", Name = "loggedin")]
        [AllowAnonymous]
        public bool LoggedIn()
        {   
            return HttpContext.User.Identity?.IsAuthenticated ?? false;
        }


        [HttpGet("tokens")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public Task<UserAuthenticationInformation> Tokens()
        {
            string sub = CookieService.GetHeaderCookieValue(CookieContants.CookieName);
            MemoryCache.TryGetValue(sub, out UserAuthenticationInformation? userInfo);
            return Task.FromResult(userInfo ?? new UserAuthenticationInformation());
        }

        [HttpGet("refreshtoken")]
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
