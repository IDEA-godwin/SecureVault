namespace SecureVault.Application.Features.Accounts.Commands;

/// <summary>
/// Command to create a new account with Name and Email.
/// </summary>
public class CreateAccountCommand : IRequest<AccountDto>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountService _accountService;

    public CreateAccountCommandHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        return await _accountService.CreateAccountAsync(new AccountRequestDto
        {
            Name = request.Name,
            Email = request.Email,
            Password = string.Empty
        }, cancellationToken);
    }
}
