using EagleBank.Entities;

namespace EagleBank.Models
{
	public class TransactionResponseDto
	{
		public int Id { get; set; }
		public TransactionType Type { get; set; }
		public decimal Amount { get; set; }
		public int AccountId { get; set; }

		public static TransactionResponseDto FromTransaction(Transaction transaction)
		{
			return new TransactionResponseDto
			{
				Id = transaction.Id,
				Type = transaction.Type,
				Amount = transaction.Amount,
				AccountId = transaction.AccountId
			};
		}
	}
}
