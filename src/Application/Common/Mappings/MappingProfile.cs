using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Entities;

namespace SecureVault.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Account mappings
        CreateMap<Account, AccountDto>()
            .ReverseMap();

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            // .ForMember(dest => dest.FromAccountName, opt => opt.MapFrom(src => src.FromAccount.AccountNumber))
            // .ForMember(dest => dest.ToAccountName, opt => opt.MapFrom(src => src.ToAccount.AccountNumber))
            .ReverseMap();
    }
}
