using dingoConfig.Contracts;
using Microsoft.Extensions.Logging;

namespace dingoConfig.Persistence.Services;

public class CatalogValidator(ILogger<CatalogValidator> logger)
{
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

        ValidateSettings(catalog.Settings, errors);

        ValidateCyclicMessage(catalog.CyclicData, errors);

        // Check for duplicate parameter names
        var duplicateParams = catalog.Settings
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
            logger.LogWarning("Catalog validation failed for {DeviceType}: {ErrorCount} errors", 
                catalog.DeviceType, errors.Count);
        }

        return result;
    }

    private void ValidateSettings(List<SettingsMessage> settings, List<ValidationError> errors)
    {
        for (int i = 0; i < settings.Count; i++)
        {
            var setting = settings[i];
            var prefix = $"Settings[{i}]";

            if (string.IsNullOrWhiteSpace(setting.Name))
                errors.Add(new ValidationError("Setting name is required", $"{prefix}.Name"));
            if (string.IsNullOrWhiteSpace(setting.DisplayName))
                errors.Add(new ValidationError("Setting display name is required", $"{prefix}.Name"));
            if (IsValidHexId(setting.IdOffset) == false)
                errors.Add(new ValidationError($"Invalid Setting ID format: {setting.IdOffset}", $"{prefix}.Id"));
            if (string.IsNullOrWhiteSpace(setting.Type))
                errors.Add(new ValidationError("Setting type is required", $"{prefix}.Type"));
            else if (!IsValidType(setting.Type))
                errors.Add(new ValidationError($"Invalid Setting type: {setting.Type}", $"{prefix}.Type"));
            
            ValidateSignal(setting.Signal, errors, prefix);
            
        }
    }

    private void ValidateCyclicMessage(List<CyclicMessage> cyclicMsg, List<ValidationError> errors)
    {
        for (int i = 0; i < cyclicMsg.Count; i++)
        {
            var cyclic = cyclicMsg[i];
            var prefix = $"CyclicMsg[{i}]";

            if (string.IsNullOrWhiteSpace(cyclic.Name))
                errors.Add(new ValidationError("Cyclic message name is required", $"{prefix}.Name"));

            if (string.IsNullOrWhiteSpace(cyclic.IdOffset))
                errors.Add(new ValidationError("Cyclic message ID is required", $"{prefix}.Id"));
            else if (!IsValidHexId(cyclic.IdOffset))
                errors.Add(new ValidationError($"Invalid cyclic message ID format: {cyclic.IdOffset}", $"{prefix}.Id"));

            ValidateSignals(cyclic.Signals, errors, prefix);
        }
    }

    private void ValidateSignals(List<SignalDefinition> signals, List<ValidationError> errors, string prefix)
    {
        for (int i = 0; i < signals.Count; i++)
        {
            ValidateSignal(signals[i], errors, $"{prefix}.Signals[{i}]");
        }
    }

    private void ValidateSignal(SignalDefinition signal, List<ValidationError> errors, string prefix)
    {

        if (string.IsNullOrWhiteSpace(signal.Name))
            errors.Add(new ValidationError("Signal name is required", $"{prefix}.Name"));
        
        if (signal.StartBit < 0 || signal.StartBit > 63)
            errors.Add(new ValidationError("Signal start bit must be between 0 and 63", $"{prefix}.StartBit"));

        if (signal.Length <= 0 || signal.Length > 64)
            errors.Add(new ValidationError("Signal length must be between 1 and 64", $"{prefix}.Length"));

        if (signal.StartBit + signal.Length > 64)
            errors.Add(new ValidationError("Signal extends beyond 64-bit boundary", $"{prefix}"));
        
        if (!IsValidByteOrder(signal.ByteOrder))
            errors.Add(new ValidationError($"Invalid byte order: {signal.ByteOrder}. Valid: littleEndian, bigEndian", $"{prefix}.ByteOrder"));
        
        ValidateSignalLimits(signal, errors, prefix);
    }

    private void ValidateSignalLimits(SignalDefinition signal, List<ValidationError> errors, string prefix)
    {
        if (signal.MinValue! != null && signal.MaxValue! != null)
        {
            try
            {
                var min = (double)signal.MinValue;
                var max = (double)signal.MaxValue;
                
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

    private static bool IsValidType(string type)
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