using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace EagleBank.Services
{
	public class TransactionService(EagleBankDbContext context, IAccountService accountService) : ITransactionService
	{
		public async Task<OneOf<TransactionResponseDto, NotFoundError, ForbiddenError, UnprocessableEntity>> CreateTransaction(int userId, int accountId, CreateTransactionDto createTransactionDto)
		{
			var accountResult = await accountService.GetAccountAsync(userId, accountId);

			if (accountResult.TryPickT1(out NotFoundError notFoundError, out var remainder))
				return notFoundError;

			if (remainder.TryPickT1(out ForbiddenError forbiddenError, out var account))
				return forbiddenError;

			if (createTransactionDto.Type == TransactionType.Withdrawal && account.Value < createTransactionDto.Amount)
				return new UnprocessableEntity(userId, accountId);

			int direction = createTransactionDto.Type == TransactionType.Withdrawal ? -1 : 1;

			Transaction transaction = new()
			{
				Amount = createTransactionDto.Amount,
				Type = createTransactionDto.Type,
				AccountId = account.Id
			};

			context.Transactions.Add(transaction);
			var accountDb = await context.Accounts.SingleAsync(a => a.Id == account.Id);
			accountDb.Value += createTransactionDto.Amount * direction;
			await context.SaveChangesAsync();
			return TransactionResponseDto.FromTransaction(transaction);
		}
	}
}
