namespace RealTimeDashboard.API.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new[] { new { id = 1, amount = -50.0, description = "Sample" } });
    }
}
