using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.EntityFrameworkCore;
using OneOf;

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

		public async Task<OneOf<AccountResponseDto, Error>> DeleteAccountAsync(int userId, int accountId)
		{
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account is null)
				return new Error(System.Net.HttpStatusCode.NotFound, $"Couldn't delete account {accountId} because it doesn't exist");

			if (account.UserId != userId)
				return new Error(System.Net.HttpStatusCode.Forbidden, $"Couldn't delete account {accountId} because it doesn't belong to {userId}");

			context.Accounts.Remove(account);
			await context.SaveChangesAsync();

			return AccountResponseDto.FromAccount(account);
		}

		public async Task<OneOf<AccountResponseDto, Error>> GetAccountAsync(int userId, int accountId)
		{
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account is null)
				return new Error(System.Net.HttpStatusCode.NotFound, $"Couldn't get account {accountId} because it doesn't exist");

			if (account.UserId != userId)
				return new Error(System.Net.HttpStatusCode.Forbidden, $"Couldn't get account {accountId} because it doesn't belong to {userId}");

			return AccountResponseDto.FromAccount(account);
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
