namespace SecureVault.Application.Features.Accounts.Commands;

/// <summary>
/// Command to credit (add funds to) an account, creating a credit transaction.
/// </summary>
public class CreditAccountCommand : IRequest<AccountDto>
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class CreditAccountCommandValidator : AbstractValidator<CreditAccountCommand>
{
    public CreditAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number must not be empty");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Credit amount must be greater than 0");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters");
    }
}

public class CreditAccountCommandHandler : IRequestHandler<CreditAccountCommand, AccountDto>
{
    private readonly IAccountService _accountService;

    public CreditAccountCommandHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<AccountDto> Handle(CreditAccountCommand request, CancellationToken cancellationToken)
    {
        return await _accountService.CreditAccountByAccountNumberAsync(request.AccountNumber, request.Amount, request.Description, cancellationToken);
    }
}
