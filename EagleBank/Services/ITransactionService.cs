using EagleBank.Entities;
using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public interface ITransactionService
	{
		Task<OneOf<TransactionResponseDto, Error>> CreateTransaction(int userId, int accountId, CreateTransactionDto createTransactionDto);
		Task<OneOf<ICollection<TransactionResponseDto>, Error>> GetTransactions(int userId, int accountId);
		Task<OneOf<TransactionResponseDto, Error>> GetTransaction(int userId, int accountId, int transactionId);
	}
}
