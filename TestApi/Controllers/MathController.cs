using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers;

[ApiController]
[Route("api/math")]
public class MathController : ControllerBase
{
    [HttpGet("add")]
    public IActionResult AddNumbers([FromQuery] double a, [FromQuery] double b)
    {
        return Ok(a + b);
    }
}
