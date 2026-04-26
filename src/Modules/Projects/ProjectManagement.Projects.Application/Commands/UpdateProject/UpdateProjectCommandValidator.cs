using FluentValidation;

namespace ProjectManagement.Projects.Application.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên project không được để trống.")
            .MaximumLength(200).WithMessage("Tên project không vượt quá 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không vượt quá 1000 ký tự.")
            .When(x => x.Description is not null);
    }
}
