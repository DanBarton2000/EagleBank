using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf;
using System.Net;

namespace EagleBank.Services
{
	public class TransactionService(EagleBankDbContext context, IAccountService accountService) : ITransactionService
	{
		public async Task<OneOf<TransactionResponseDto, Error>> CreateTransaction(int userId, int accountId, CreateTransactionDto createTransactionDto)
		{
			var account = await context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId);

			if (account == null)
				return new Error(HttpStatusCode.NotFound, $"Could not find account with id {accountId}");

			if (account.UserId != userId)
				return new Error(HttpStatusCode.Forbidden, $"Tried to access account {accountId}, but it isn't an account of {userId}");

			if (createTransactionDto.Type == TransactionType.Withdrawal && account.Value < createTransactionDto.Amount)
				return new Error(HttpStatusCode.UnprocessableEntity, $"Not enough funds (£{account.Value}) in account {accountId} to withdraw £{createTransactionDto.Amount}");

			int direction = createTransactionDto.Type == TransactionType.Withdrawal ? -1 : 1;

			Transaction transaction = createTransactionDto.ToTransaction();
			transaction.AccountId = account.Id;

			context.Transactions.Add(transaction);
			account.Value += transaction.Amount * direction;
			await context.SaveChangesAsync();
			return TransactionResponseDto.FromTransaction(transaction);
		}

		public async Task<OneOf<TransactionResponseDto, Error>> GetTransaction(int userId, int accountId, int transactionId)
		{
			var accountResult = await accountService.GetAccountAsync(userId, accountId);

			if (accountResult.TryPickT1(out Error error, out var remainder))
				return error;

			Transaction? transaction = await context.Transactions.Where(t => t.AccountId == accountId).SingleOrDefaultAsync(t => t.Id == transactionId);

			if (transaction is null)
				return new Error(HttpStatusCode.NotFound, $"Transaction {transactionId} for account {accountId} does not exist");

			return TransactionResponseDto.FromTransaction(transaction);
		}

		public async Task<OneOf<ICollection<TransactionResponseDto>, Error>> GetTransactions(int userId, int accountId)
		{
			var accountResult = await accountService.GetAccountAsync(userId, accountId);

			if (accountResult.TryPickT1(out Error error, out var account))
				return error;

			return await context.Transactions.Where(t => t.AccountId == account.Id).Select(t => TransactionResponseDto.FromTransaction(t)).ToListAsync();
		}
	}
}
