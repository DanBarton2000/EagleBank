using EagleBank.Entities;
using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public interface ITransactionService
	{
		Task<OneOf<TransactionResponseDto, NotFoundError, ForbiddenError, UnprocessableEntity>> CreateTransaction(int userId, int accountId, CreateTransactionDto createTransactionDto);
	}
}
