namespace SecureVault.Application.Features.Accounts.Commands;

public class DeleteAccountCommand : IRequest
{
    public string AccountNumber { get; set; } = string.Empty;
}

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number must not be empty");
    }
}

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IAccountService _accountService;

    public DeleteAccountCommandHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        await _accountService.DeleteAccountByAccountNumberAsync(request.AccountNumber, cancellationToken);
    }
}
