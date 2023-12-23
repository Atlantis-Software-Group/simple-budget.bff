using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace simple_budget.bff.Controllers
{
    [Route("[controller]/[action]")]    
    public class UserController : ControllerBase
    {
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
    }
}
