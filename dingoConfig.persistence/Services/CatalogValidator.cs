using dingoConfig.Contracts;
using Microsoft.Extensions.Logging;

namespace dingoConfig.Persistence.Services;

public class CatalogValidator
{
    private readonly ILogger<CatalogValidator> _logger;

    public CatalogValidator(ILogger<CatalogValidator> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateCatalog(DeviceCatalog catalog)
    {
        var errors = new List<ValidationError>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(catalog.DeviceType))
            errors.Add(new ValidationError("DeviceType is required", nameof(catalog.DeviceType)));

        if (string.IsNullOrWhiteSpace(catalog.Version))
            errors.Add(new ValidationError("Version is required", nameof(catalog.Version)));

        if (string.IsNullOrWhiteSpace(catalog.Manufacturer))
            errors.Add(new ValidationError("Manufacturer is required", nameof(catalog.Manufacturer)));

        // Validate communication info
        ValidateCommunicationInfo(catalog.Communication, errors);

        // Validate parameters
        ValidateParameters(catalog.Parameters, errors);

        // Validate cyclic data definitions
        ValidateCyclicData(catalog.CyclicData, errors);

        // Check for duplicate parameter names
        var duplicateParams = catalog.Parameters
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicateParams)
        {
            errors.Add(new ValidationError($"Duplicate parameter name: {duplicate}", "Parameters"));
        }

        var result = errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        
        if (!result.IsValid)
        {
            _logger.LogWarning("Catalog validation failed for {DeviceType}: {ErrorCount} errors", 
                catalog.DeviceType, errors.Count);
        }

        return result;
    }

    private void ValidateCommunicationInfo(CommunicationInfo communication, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(communication.Type))
        {
            errors.Add(new ValidationError("Communication type is required", "Communication.Type"));
        }
        else if (!IsValidCommunicationType(communication.Type))
        {
            errors.Add(new ValidationError($"Invalid communication type: {communication.Type}. Valid types: CAN, USB_CDC", "Communication.Type"));
        }

        if (communication.Type == "CAN" && string.IsNullOrWhiteSpace(communication.BaseId))
        {
            errors.Add(new ValidationError("BaseId is required for CAN communication", "Communication.BaseId"));
        }

        if (!string.IsNullOrWhiteSpace(communication.BaseId) && !IsValidHexId(communication.BaseId))
        {
            errors.Add(new ValidationError($"Invalid BaseId format: {communication.BaseId}. Expected hex format like '0x600'", "Communication.BaseId"));
        }
    }

    private void ValidateParameters(List<DeviceParameter> parameters, List<ValidationError> errors)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var prefix = $"Parameters[{i}]";

            if (string.IsNullOrWhiteSpace(param.Name))
                errors.Add(new ValidationError("Parameter name is required", $"{prefix}.Name"));

            if (string.IsNullOrWhiteSpace(param.Type))
                errors.Add(new ValidationError("Parameter type is required", $"{prefix}.Type"));
            else if (!IsValidParameterType(param.Type))
                errors.Add(new ValidationError($"Invalid parameter type: {param.Type}", $"{prefix}.Type"));

            if (string.IsNullOrWhiteSpace(param.Address))
                errors.Add(new ValidationError("Parameter address is required", $"{prefix}.Address"));
            else if (!IsValidHexId(param.Address))
                errors.Add(new ValidationError($"Invalid address format: {param.Address}", $"{prefix}.Address"));

            if (!IsValidByteOrder(param.ByteOrder))
                errors.Add(new ValidationError($"Invalid byte order: {param.ByteOrder}. Valid: littleEndian, bigEndian", $"{prefix}.ByteOrder"));

            // Validate limits if present
            if (param.Limits != null)
            {
                ValidateParameterLimits(param, errors, prefix);
            }
        }
    }

    private void ValidateCyclicData(List<CyclicDataDefinition> cyclicData, List<ValidationError> errors)
    {
        for (int i = 0; i < cyclicData.Count; i++)
        {
            var cyclic = cyclicData[i];
            var prefix = $"CyclicData[{i}]";

            if (string.IsNullOrWhiteSpace(cyclic.Name))
                errors.Add(new ValidationError("Cyclic data name is required", $"{prefix}.Name"));

            if (string.IsNullOrWhiteSpace(cyclic.Id))
                errors.Add(new ValidationError("Cyclic data ID is required", $"{prefix}.Id"));
            else if (!IsValidHexId(cyclic.Id))
                errors.Add(new ValidationError($"Invalid cyclic data ID format: {cyclic.Id}", $"{prefix}.Id"));

            if (cyclic.Interval <= 0)
                errors.Add(new ValidationError("Cyclic data interval must be positive", $"{prefix}.Interval"));

            // Validate signals
            ValidateSignals(cyclic.Signals, errors, prefix);
        }
    }

    private void ValidateSignals(List<SignalDefinition> signals, List<ValidationError> errors, string prefix)
    {
        for (int i = 0; i < signals.Count; i++)
        {
            var signal = signals[i];
            var signalPrefix = $"{prefix}.Signals[{i}]";

            if (string.IsNullOrWhiteSpace(signal.Name))
                errors.Add(new ValidationError("Signal name is required", $"{signalPrefix}.Name"));

            if (signal.StartBit < 0 || signal.StartBit > 63)
                errors.Add(new ValidationError("Signal start bit must be between 0 and 63", $"{signalPrefix}.StartBit"));

            if (signal.Length <= 0 || signal.Length > 64)
                errors.Add(new ValidationError("Signal length must be between 1 and 64", $"{signalPrefix}.Length"));

            if (signal.StartBit + signal.Length > 64)
                errors.Add(new ValidationError("Signal extends beyond 64-bit boundary", $"{signalPrefix}"));

            if (!IsValidByteOrder(signal.ByteOrder))
                errors.Add(new ValidationError($"Invalid byte order: {signal.ByteOrder}", $"{signalPrefix}.ByteOrder"));

            if (string.IsNullOrWhiteSpace(signal.Type))
                errors.Add(new ValidationError("Signal type is required", $"{signalPrefix}.Type"));
            else if (!IsValidParameterType(signal.Type))
                errors.Add(new ValidationError($"Invalid signal type: {signal.Type}", $"{signalPrefix}.Type"));
        }
    }

    private void ValidateParameterLimits(DeviceParameter param, List<ValidationError> errors, string prefix)
    {
        if (param.Limits!.Min != null && param.Limits.Max != null)
        {
            try
            {
                // Handle JSON number parsing - they come as JsonElement
                double min, max;
                
                if (param.Limits.Min is System.Text.Json.JsonElement minElement)
                {
                    min = minElement.GetDouble();
                }
                else
                {
                    min = Convert.ToDouble(param.Limits.Min);
                }
                
                if (param.Limits.Max is System.Text.Json.JsonElement maxElement)
                {
                    max = maxElement.GetDouble();
                }
                else
                {
                    max = Convert.ToDouble(param.Limits.Max);
                }
                
                if (min >= max)
                {
                    errors.Add(new ValidationError("Parameter minimum value must be less than maximum", $"{prefix}.Limits"));
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError($"Invalid limit values - must be numeric: {ex.Message}", $"{prefix}.Limits"));
            }
        }
    }

    private static bool IsValidCommunicationType(string type)
    {
        return type is "CAN" or "USB_CDC";
    }

    private static bool IsValidParameterType(string type)
    {
        return type is "uint8" or "uint16" or "uint32" or "int8" or "int16" or "int32" or "float" or "double" or "bool";
    }

    private static bool IsValidByteOrder(string byteOrder)
    {
        return byteOrder is "littleEndian" or "bigEndian";
    }

    private static bool IsValidHexId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (!id.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return false;

        var hexPart = id[2..];
        return hexPart.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}