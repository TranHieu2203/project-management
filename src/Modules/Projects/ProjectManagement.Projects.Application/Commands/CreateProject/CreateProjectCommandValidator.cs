using FluentValidation;

namespace ProjectManagement.Projects.Application.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code không được để trống.")
            .MaximumLength(20).WithMessage("Code không vượt quá 20 ký tự.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Code chỉ dùng chữ hoa, số, gạch ngang.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên project không được để trống.")
            .MaximumLength(200).WithMessage("Tên project không vượt quá 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không vượt quá 1000 ký tự.")
            .When(x => x.Description is not null);
    }
}
