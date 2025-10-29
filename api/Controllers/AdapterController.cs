using contracts.Adapters;
using domain.Enums;
using domain.Interfaces;
using infrastructure.Comms.Adapters;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

public class AdapterController(
    ILogger<AdapterController> logger,
    IServiceProvider serviceProvider,
    ICommsAdapterManager adapterManager)
    : ControllerBase
{
    private readonly ICommsAdapterManager _adapterManager = adapterManager;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AdapterController> _logger = logger;

    [HttpGet("available")]
    public ActionResult<List<string>> GetAvailableAdapters()
    {
        return Ok(new List<string> { "USB", "SLCAN", "PCAN", "Sim" });
    }

    [HttpGet("status")]
    public ActionResult<AdapterStatusResponse> GetStatus()
    {
        return Ok(new AdapterStatusResponse
        {
            IsConnected = _adapterManager.IsConnected,
            AdapterName = nameof(_adapterManager.ActiveAdapter)
        });
    }

    [HttpPost("connect")]
    public async Task<ActionResult> Connect([FromBody] ConnectAdapterRequest request)
    {
        ICommsAdapter adapter = request.AdapterType switch
        {
            "USB" => _serviceProvider.GetRequiredService<UsbAdapter>(),
            "SLCAN" => _serviceProvider.GetRequiredService<SlcanAdapter>(),
            "PCAN" => _serviceProvider.GetRequiredService<PcanAdapter>(),
            "Sim" => _serviceProvider.GetRequiredService<SimAdapter>(),
            _ => throw new ArgumentException($"Unknown adapter type: {request.AdapterType}")
        };

        CanBitRate br = request.Bitrate switch
        {
            "1000K" => CanBitRate.BitRate1000K,
            "500K" => CanBitRate.BitRate500K,
            "250K" => CanBitRate.BitRate250K,
            "125K" => CanBitRate.BitRate125K,
            _ => throw new ArgumentException($"Unknown bitrate type: {request.Bitrate}")
        };

        var success = await _adapterManager.ConnectAsync(adapter, request.Port, br, CancellationToken.None);
        
        if(!success) return BadRequest("Failed to connect to adapter");
        
        _logger.LogInformation($"Adapter connected: {nameof(_adapterManager.ActiveAdapter)}");
        
        return Ok();
    }
    
    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect()
    {
        await _adapterManager.DisconnectAsync();
        _logger.LogInformation("Adapter disconnected");
        return Ok();
    }
}