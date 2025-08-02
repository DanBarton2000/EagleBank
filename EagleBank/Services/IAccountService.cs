using EagleBank.Entities;
using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public record NotFoundError(int AccountId);
	public record ForbiddenError(int AccountId, int UserId);

	public interface IAccountService
	{
		Task<AccountResponseDto?> CreateAsync(AccountDto request, int userId);
		Task<ICollection<AccountResponseDto>?> GetAccountsAsync(int nameId);
		Task<Account?> GetAccountAsync(int accountId);
		Task<OneOf<Account, NotFoundError, ForbiddenError>> DeleteAccountAsync(int userId, int accountId);
	}
}
