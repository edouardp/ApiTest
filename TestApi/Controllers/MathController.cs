using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace TestApi.Controllers;

public record AdditionResult(double sum);

[ApiController]
[Route("api/math")]
public class MathController : ControllerBase
{
    [HttpGet("add")]
    public IActionResult AddNumbers([FromQuery] double a, [FromQuery] double b)
    {
        return Ok(a+b);
    }
}
