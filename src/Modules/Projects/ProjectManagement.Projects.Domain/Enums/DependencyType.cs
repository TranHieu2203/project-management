namespace ProjectManagement.Projects.Domain.Enums;

public enum DependencyType
{
    FS, // Finish-to-Start (default)
    SS, // Start-to-Start
    FF, // Finish-to-Finish
    SF  // Start-to-Finish
}
