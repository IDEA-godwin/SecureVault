
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.Features.Accounts.Queries;

public class GetAccountByAccountNumberQuery : IRequest<AccountDto>
{
    public string AccountNumber { get; set; } = string.Empty;
}

public class GetAccountByAccountNumberQueryHandler : IRequestHandler<GetAccountByAccountNumberQuery, AccountDto>
{
    private readonly IAccountService _accountService;

    public GetAccountByAccountNumberQueryHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<AccountDto> Handle(GetAccountByAccountNumberQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByAccountNumberAsync(request.AccountNumber, cancellationToken);
        
        if (account == null)
        {
            throw new AppNotFoundException($"Account with account number {request.AccountNumber} was not found.");
        }

        return account;
    }
}
