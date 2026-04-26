using MediatR;

namespace ProjectManagement.Workforce.Application.Rates.Commands.DeleteRate;

public sealed record DeleteRateCommand(Guid RateId, string DeletedBy) : IRequest;
