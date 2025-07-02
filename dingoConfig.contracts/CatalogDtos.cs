namespace dingoConfig.Contracts.Dtos;

public class CatalogListDto
{
    public List<CatalogSummaryDto> Catalogs { get; set; } = new();
}

public class CatalogSummaryDto
{
    public string DeviceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string FilePath { get; set; } = string.Empty;
}

public class CatalogValidationRequest
{
    public string? FilePath { get; set; }
    public string? CatalogJson { get; set; }
}

public class CatalogValidationResponse
{
    public bool IsValid { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new();
    public CatalogSummaryDto? CatalogInfo { get; set; }
}

public class ValidationErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string Severity { get; set; } = "Error"; // Error, Warning, Info
}