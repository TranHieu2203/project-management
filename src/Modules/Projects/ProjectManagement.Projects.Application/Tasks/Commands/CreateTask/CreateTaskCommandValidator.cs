using FluentValidation;

namespace ProjectManagement.Projects.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Vbs).MaximumLength(50).When(x => x.Vbs is not null);
        RuleFor(x => x.PlannedEffortHours)
            .GreaterThan(0)
            .When(x => x.PlannedEffortHours.HasValue)
            .WithMessage("plannedEffortHours phải lớn hơn 0.");
        RuleFor(x => x.PercentComplete)
            .InclusiveBetween(0, 100)
            .When(x => x.PercentComplete.HasValue)
            .WithMessage("percentComplete phải nằm trong khoảng 0–100.");

        // Date validation: plannedStartDate <= plannedEndDate
        RuleFor(x => x)
            .Must(x => x.PlannedStartDate is null || x.PlannedEndDate is null
                       || x.PlannedStartDate <= x.PlannedEndDate)
            .WithMessage("plannedStartDate phải nhỏ hơn hoặc bằng plannedEndDate.");

        // Date validation: actualStartDate <= actualEndDate
        RuleFor(x => x)
            .Must(x => x.ActualStartDate is null || x.ActualEndDate is null
                       || x.ActualStartDate <= x.ActualEndDate)
            .WithMessage("actualStartDate phải nhỏ hơn hoặc bằng actualEndDate.");
    }
}
