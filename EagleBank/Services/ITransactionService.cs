using EagleBank.Entities;
using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public interface ITransactionService
	{
		Task<OneOf<TransactionResponseDto, NotFoundError, ForbiddenError, UnprocessableEntity>> CreateTransaction(int userId, int accountId, CreateTransactionDto createTransactionDto);
		Task<OneOf<ICollection<TransactionResponseDto>, NotFoundError, ForbiddenError>> GetTransactions(int userId, int accountId);
		Task<OneOf<TransactionResponseDto, NotFoundError, ForbiddenError>> GetTransaction(int userId, int accountId);
	}
}
