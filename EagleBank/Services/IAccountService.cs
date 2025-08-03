using EagleBank.Entities;
using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public interface IAccountService
	{
		Task<AccountResponseDto?> CreateAsync(AccountDto request, int userId);
		Task<ICollection<AccountResponseDto>?> GetAccountsAsync(int userId);
		Task<OneOf<AccountResponseDto, NotFoundError, ForbiddenError>> GetAccountAsync(int userId, int accountId);
		Task<OneOf<AccountResponseDto, NotFoundError, ForbiddenError>> DeleteAccountAsync(int userId, int accountId);
	}
}
