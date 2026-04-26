using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Domain.Entities;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Application.Rates.Commands.CreateRate;

public sealed class CreateRateHandler : IRequestHandler<CreateRateCommand, MonthlyRateDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public CreateRateHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<MonthlyRateDto> Handle(CreateRateCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<ResourceRole>(cmd.Role, out _))
            throw new DomainException($"Role không hợp lệ: '{cmd.Role}'.");

        if (!Enum.TryParse<ResourceLevel>(cmd.Level, out _))
            throw new DomainException($"Level không hợp lệ: '{cmd.Level}'.");

        if (cmd.Month < 1 || cmd.Month > 12)
            throw new DomainException("Month phải từ 1 đến 12.");

        if (cmd.MonthlyAmount <= 0)
            throw new DomainException("MonthlyAmount phải lớn hơn 0.");

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == cmd.VendorId, ct)
            ?? throw new DomainException("Vendor không tồn tại.");

        if (!vendor.IsActive)
            throw new DomainException($"Vendor '{vendor.Name}' đã inactive.");

        var exists = await _db.MonthlyRates.AnyAsync(r =>
            r.VendorId == cmd.VendorId &&
            r.Role == cmd.Role &&
            r.Level == cmd.Level &&
            r.Year == cmd.Year &&
            r.Month == cmd.Month, ct);

        if (exists)
            throw new ConflictException(
                $"Rate cho vendor/role '{cmd.Role}'/level '{cmd.Level}' tháng {cmd.Month}/{cmd.Year} đã tồn tại.");

        var rate = MonthlyRate.Create(
            cmd.VendorId, cmd.Role, cmd.Level,
            cmd.Year, cmd.Month, cmd.MonthlyAmount, cmd.CreatedBy);

        _db.MonthlyRates.Add(rate);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Rate", rate.Id, "Create", cmd.CreatedBy,
            $"Created rate: {vendor.Name} / {cmd.Role} / {cmd.Level} / {cmd.Month}/{cmd.Year} = {cmd.MonthlyAmount}"), ct);

        return ToDto(rate, vendor.Name);
    }

    internal static MonthlyRateDto ToDto(MonthlyRate r, string? vendorName = null) => new(
        r.Id, r.VendorId, vendorName ?? r.Vendor?.Name,
        r.Role, r.Level, r.Year, r.Month,
        r.MonthlyAmount, r.HourlyRate,
        r.CreatedAt, r.CreatedBy);
}
