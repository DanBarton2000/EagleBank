using EagleBank.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace EagleBank.Models
{
	public class CreateTransactionDto
	{
		[Required]
		public TransactionType? Type { get; set; }
		[Required]
		public decimal? Amount { get; set; }

		public Transaction ToTransaction()
		{
			if (Type == null || Amount == null)
				throw new InvalidOperationException("Cannot convert DTO to Transaction: Missing required fields");

			return new()
			{
				Type = (TransactionType)Type,
				Amount = (decimal)Amount
			};
		}
	}
}
