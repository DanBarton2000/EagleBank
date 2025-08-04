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
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account == null)
				return new NotFoundError(accountId);

			if (account.UserId != userId)
				return new ForbiddenError(userId, accountId);

			if (createTransactionDto.Type == TransactionType.Withdrawal && account.Value < createTransactionDto.Amount)
				return new UnprocessableEntity(userId, accountId);

			int direction = createTransactionDto.Type == TransactionType.Withdrawal ? -1 : 1;

			Transaction transaction = createTransactionDto.ToTransaction();
			transaction.AccountId = account.Id;

			context.Transactions.Add(transaction);
			account.Value += transaction.Amount * direction;
			await context.SaveChangesAsync();
			return TransactionResponseDto.FromTransaction(transaction);
		}

		public async Task<OneOf<TransactionResponseDto, NotFoundError, ForbiddenError>> GetTransaction(int userId, int accountId, int transactionId)
		{
			var accountResult = await accountService.GetAccountAsync(userId, accountId);

			if (accountResult.TryPickT1(out NotFoundError notFoundError, out var remainder))
				return notFoundError;

			if (remainder.TryPickT1(out ForbiddenError forbiddenError, out var account))
				return forbiddenError;

			Transaction? transaction = await context.Transactions.Where(t => t.AccountId == accountId).SingleOrDefaultAsync(t => t.Id == transactionId);

			if (transaction is null)
				return new NotFoundError(accountId);

			return TransactionResponseDto.FromTransaction(transaction);
		}

		public async Task<OneOf<ICollection<TransactionResponseDto>, NotFoundError, ForbiddenError>> GetTransactions(int userId, int accountId)
		{
			var accountResult = await accountService.GetAccountAsync(userId, accountId);

			if (accountResult.TryPickT1(out NotFoundError notFoundError, out var remainder))
				return notFoundError;

			if (remainder.TryPickT1(out ForbiddenError forbiddenError, out var account))
				return forbiddenError;

			return await context.Transactions.Where(t => t.AccountId == account.Id).Select(t => TransactionResponseDto.FromTransaction(t)).ToListAsync();
		}
	}
}
