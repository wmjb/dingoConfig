namespace dingoConfig.Contracts;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    
    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(string message, string? propertyName = null)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError> { new(message, propertyName) }
        };
    }
    
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}

public class ValidationError
{
    public string Message { get; set; }
    public string? PropertyName { get; set; }
    
    public ValidationError(string message, string? propertyName = null)
    {
        Message = message;
        PropertyName = propertyName;
    }
}