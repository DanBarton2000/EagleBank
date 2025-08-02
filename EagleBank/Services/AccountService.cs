using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.EntityFrameworkCore;

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

		public async Task<ICollection<AccountResponseDto>?> GetAccounts(int userId)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Id == userId);
			if (user is null)
				return null;

			var accounts = context.Accounts.Where(a => a.UserId == userId);

			List<AccountResponseDto> rtn = [];

			foreach (Account account in accounts)
			{
				rtn.Add(AccountResponseDto.FromAccount(account));
			}

			return rtn;
		}
	}
}
