using Microsoft.AspNetCore.Mvc;
using dingoConfig.Contracts;
using dingoConfig.Contracts.Dtos;
using dingoConfig.Persistence.Interfaces;

namespace dingoConfig.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogsController : ControllerBase
{
    private readonly IDeviceCatalogService _catalogService;
    private readonly ILogger<CatalogsController> _logger;

    public CatalogsController(IDeviceCatalogService catalogService, ILogger<CatalogsController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all available device catalogs
    /// </summary>
    [HttpGet]
    public ActionResult<CatalogListDto> GetCatalogs()
    {
        try
        {
            if (!_catalogService.IsLoaded)
            {
                return BadRequest(new { message = "Catalogs not loaded. Please ensure catalog directory is configured." });
            }

            var catalogs = _catalogService.GetAllCatalogs()
                .Select(c => new CatalogSummaryDto
                {
                    DeviceType = c.DeviceType,
                    Version = c.Version,
                    Manufacturer = c.Manufacturer,
                    Description = c.Description,
                    LastModified = _catalogService.LastLoadTime,
                    FilePath = $"catalogs/{c.DeviceType}.json"
                })
                .ToList();

            return Ok(new CatalogListDto { Catalogs = catalogs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog list");
            return StatusCode(500, new { message = "Internal server error retrieving catalogs" });
        }
    }

    /// <summary>
    /// Get specific device catalog by device type
    /// </summary>
    [HttpGet("{deviceType}")]
    public ActionResult<DeviceCatalog> GetCatalog(string deviceType)
    {
        try
        {
            if (!_catalogService.IsLoaded)
            {
                return BadRequest(new { message = "Catalogs not loaded" });
            }

            var catalog = _catalogService.GetCatalog(deviceType);
            if (catalog == null)
            {
                return NotFound(new { message = $"Catalog not found for device type: {deviceType}" });
            }

            return Ok(catalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog for device type {DeviceType}", deviceType);
            return StatusCode(500, new { message = "Internal server error retrieving catalog" });
        }
    }

    /// <summary>
    /// Validate a catalog file or JSON content
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<CatalogValidationResponse>> ValidateCatalog([FromBody] CatalogValidationRequest request)
    {
        try
        {
            ValidationResult validationResult;

            if (!string.IsNullOrEmpty(request.FilePath))
            {
                validationResult = await _catalogService.ValidateCatalogAsync(request.FilePath);
            }
            else if (!string.IsNullOrEmpty(request.CatalogJson))
            {
                validationResult = await _catalogService.ValidateCatalogJsonAsync(request.CatalogJson);
            }
            else
            {
                return BadRequest(new { message = "Either FilePath or CatalogJson must be provided" });
            }

            var response = new CatalogValidationResponse
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.Select(e => new ValidationErrorDto
                {
                    Message = e.Message,
                    PropertyName = e.PropertyName,
                    Severity = "Error"
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating catalog");
            return StatusCode(500, new { message = "Internal server error during validation" });
        }
    }

    /// <summary>
    /// Get available device types
    /// </summary>
    [HttpGet("types")]
    public ActionResult<IEnumerable<string>> GetDeviceTypes()
    {
        try
        {
            if (!_catalogService.IsLoaded)
            {
                return BadRequest(new { message = "Catalogs not loaded" });
            }

            var deviceTypes = _catalogService.GetAvailableDeviceTypes();
            return Ok(deviceTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device types");
            return StatusCode(500, new { message = "Internal server error retrieving device types" });
        }
    }

    /// <summary>
    /// Reload catalogs from the configured directory
    /// </summary>
    [HttpPost("reload")]
    public async Task<ActionResult> ReloadCatalogs()
    {
        try
        {
            await _catalogService.ReloadCatalogsAsync();
            
            var catalogCount = _catalogService.GetAvailableDeviceTypes().Count();
            return Ok(new { message = $"Successfully reloaded {catalogCount} catalogs", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading catalogs");
            return StatusCode(500, new { message = "Internal server error reloading catalogs" });
        }
    }

    /// <summary>
    /// Get catalog loading status and information
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetCatalogStatus()
    {
        try
        {
            var status = new
            {
                IsLoaded = _catalogService.IsLoaded,
                LastLoadTime = _catalogService.LastLoadTime,
                CatalogCount = _catalogService.IsLoaded ? _catalogService.GetAvailableDeviceTypes().Count() : 0,
                AvailableTypes = _catalogService.IsLoaded ? _catalogService.GetAvailableDeviceTypes() : new List<string>()
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog status");
            return StatusCode(500, new { message = "Internal server error retrieving status" });
        }
    }
}