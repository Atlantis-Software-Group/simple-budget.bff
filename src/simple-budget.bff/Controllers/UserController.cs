using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace simple_budget.bff.Controllers
{
    [Route("[controller]/[action]")]    
    public class UserController : ControllerBase
    {
        [HttpGet]
        public async Task Login([FromQuery]string ReturnUrl)
        {   
            bool challenge = true;         
            string? access_token = await HttpContext.GetTokenAsync("access_token");
            if ( !string.IsNullOrWhiteSpace(access_token) )
            {
                JwtSecurityToken jwtToken = new JwtSecurityToken(access_token);
                challenge = (jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow);
            }

            if ( challenge )
                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public Task<string> Logout()
        {
            return Task.FromResult("Logout");
        }

        [HttpGet]
        public Task NotAuthorized()
        {
            return Task.FromResult(Forbid(CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
