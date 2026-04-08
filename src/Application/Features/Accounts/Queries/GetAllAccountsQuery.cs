namespace SecureVault.Application.Features.Accounts.Queries;

public class GetAllAccountsQuery : IRequest<List<AccountDto>>
{
}

public class GetAllAccountsQueryHandler : IRequestHandler<GetAllAccountsQuery, List<AccountDto>>
{
    private readonly IAccountService _accountService;

    public GetAllAccountsQueryHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<List<AccountDto>> Handle(GetAllAccountsQuery request, CancellationToken cancellationToken)
    {
        return await _accountService.GetAllAccountsAsync(cancellationToken);
    }
}
