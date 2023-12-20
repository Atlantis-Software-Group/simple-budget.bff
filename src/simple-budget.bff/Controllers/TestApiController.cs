using Microsoft.AspNetCore.Mvc;

namespace simple_budget.bff;

[Route("[controller]/[action]")]    
[ApiController]
public class TestApiController : ControllerBase
{
    [HttpGet]
    public Task<string> Hello()
    {
        return Task.FromResult("hi");
    }
}
