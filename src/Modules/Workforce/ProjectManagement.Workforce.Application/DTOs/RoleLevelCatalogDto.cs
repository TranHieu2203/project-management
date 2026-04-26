namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record RoleLevelCatalogDto(
    List<LookupItemDto> Roles,
    List<LookupItemDto> Levels
);
