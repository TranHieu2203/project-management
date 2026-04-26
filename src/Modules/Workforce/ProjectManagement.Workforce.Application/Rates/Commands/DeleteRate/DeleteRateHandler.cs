using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Notifications;

namespace ProjectManagement.Workforce.Application.Rates.Commands.DeleteRate;

public sealed class DeleteRateHandler : IRequestHandler<DeleteRateCommand>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public DeleteRateHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task Handle(DeleteRateCommand cmd, CancellationToken ct)
    {
        var rate = await _db.MonthlyRates.FirstOrDefaultAsync(r => r.Id == cmd.RateId, ct)
            ?? throw new NotFoundException("Rate không tồn tại.");

        var rateId = rate.Id;
        _db.MonthlyRates.Remove(rate);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Rate", rateId, "Delete", cmd.DeletedBy,
            $"Deleted rate id: {cmd.RateId}"), ct);
    }
}
