using MediatR;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Application.Lookups.Queries.GetRoleLevelCatalog;

public sealed class GetRoleLevelCatalogHandler : IRequestHandler<GetRoleLevelCatalogQuery, RoleLevelCatalogDto>
{
    public Task<RoleLevelCatalogDto> Handle(GetRoleLevelCatalogQuery query, CancellationToken ct)
    {
        var roles = Enum.GetValues<ResourceRole>()
            .Select(r => new LookupItemDto(r.ToString(), ToLabel(r)))
            .ToList();

        var levels = Enum.GetValues<ResourceLevel>()
            .Select(l => new LookupItemDto(l.ToString(), ToLabel(l)))
            .ToList();

        return Task.FromResult(new RoleLevelCatalogDto(roles, levels));
    }

    private static string ToLabel(ResourceRole r) => r switch
    {
        ResourceRole.Developer => "Developer",
        ResourceRole.PM        => "Project Manager",
        ResourceRole.QA        => "QA Engineer",
        ResourceRole.BA        => "Business Analyst",
        ResourceRole.DevOps    => "DevOps Engineer",
        ResourceRole.Designer  => "UI/UX Designer",
        ResourceRole.TechLead  => "Tech Lead",
        ResourceRole.Architect => "Solutions Architect",
        _                      => r.ToString()
    };

    private static string ToLabel(ResourceLevel l) => l switch
    {
        ResourceLevel.Junior    => "Junior",
        ResourceLevel.Mid       => "Mid-level",
        ResourceLevel.Senior    => "Senior",
        ResourceLevel.Lead      => "Lead",
        ResourceLevel.Principal => "Principal",
        _                       => l.ToString()
    };
}
