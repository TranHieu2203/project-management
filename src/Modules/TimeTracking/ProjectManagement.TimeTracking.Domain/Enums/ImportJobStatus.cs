namespace ProjectManagement.TimeTracking.Domain.Enums;

public enum ImportJobStatus
{
    Pending,
    Validating,
    ValidatedOk,
    ValidatedWithWarnings,
    ValidatedWithErrors,
    Applying,
    Completed,
    Failed
}
