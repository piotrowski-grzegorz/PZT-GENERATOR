namespace PztGenerator;

public sealed record ValidationMessage(string Text, ValidationSeverity Severity);

public enum ValidationSeverity
{
    Info,
    Success,
    Warning,
    Error
}
