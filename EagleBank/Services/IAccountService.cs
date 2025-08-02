using EagleBank.Entities;
using EagleBank.Models;

namespace EagleBank.Services
{
	public interface IAccountService
	{
		Task<AccountResponseDto?> CreateAsync(AccountDto request, int userId);
		Task<ICollection<AccountResponseDto>?> GetAccounts(int nameId);
		Task<Account?> GetAccount(int accountId);
	}
}
