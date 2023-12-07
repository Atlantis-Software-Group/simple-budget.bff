using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace simple_budget.bff.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        [HttpPost]
        public Task<string> Login()
        {
            return Task.FromResult("Login");
        }

        [HttpPost]
        public Task<string> Logout()
        {
            return Task.FromResult("Logout");
        }
    }
}
