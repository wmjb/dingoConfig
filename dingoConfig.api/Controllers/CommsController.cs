using Microsoft.AspNetCore.Mvc;

namespace dingoConfig.api.Controllers;

[ApiController]
[Route("[controller]")]
public class CommsController(ILogger<CommsController> logger) : ControllerBase
{
    private static readonly string[] CommTypes =
    [
        "SLCAN", "USB", "PEAK"
    ];
    
    private readonly ILogger<CommsController> _logger = logger;

    [HttpGet(Name = "GetComms")]
    public IEnumerable<string> Get()
    {
        return CommTypes;
    }

    [HttpGet("status", Name = "GetStatus")]
    public string GetStatus()
    {
        return "OK";
    }

    [HttpPost("connect", Name = "PostConnect")]
    public IActionResult PostConnect()
    {
        return Ok();
    }
    
    [HttpPost("disconnect", Name = "PostDisconnect")]
    public IActionResult PostDisconnect()
    {
        return Ok();
    }
}