using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.EntityFrameworkCore;
using OneOf;
using System.Threading.Tasks;

namespace EagleBank.Services
{
	public class AccountService(EagleBankDbContext context) : IAccountService
	{
		public async Task<AccountResponseDto?> CreateAsync(AccountDto request, int userId)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Id == userId);
			if (user is null)
				return null;

			Account account = new()
			{
				Type = request.Type,
				Value = 0m,
				User = user
			};

			context.Accounts.Add(account);
			await context.SaveChangesAsync();

			return AccountResponseDto.FromAccount(account);
		}

		public async Task<OneOf<Account, NotFoundError, ForbiddenError>> DeleteAccountAsync(int userId, int accountId)
		{
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account is null) 
				return new NotFoundError(accountId);

			if (account.UserId != userId)
				return new ForbiddenError(accountId, userId);

			context.Accounts.Remove(account);
			await context.SaveChangesAsync();

			return account;
		}

		public async Task<Account?> GetAccountAsync(int accountId)
		{
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account is null)
				return null;

			return account;
		}

		public async Task<ICollection<AccountResponseDto>?> GetAccountsAsync(int userId)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Id == userId);
			if (user is null)
				return null;

			var accounts = await context.Accounts.Where(a => a.UserId == userId).ToListAsync();

			List<AccountResponseDto> rtn = [];

			foreach (Account account in accounts)
			{
				rtn.Add(AccountResponseDto.FromAccount(account));
			}

			return rtn;
		}
	}
}
