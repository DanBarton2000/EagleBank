using EagleBank.Entities;

namespace EagleBank.Models
{
	public class CreateTransactionDto
	{
		public TransactionType Type { get; set; }
		public decimal Amount { get; set; }
	}
}
