using EagleBank.Entities;

namespace EagleBank.Models
{
	public class AccountResponseDto
	{
		public int Id { get; set; }
		public AccountType Type { get; set; }
		public decimal Value { get; set; }

		public static AccountResponseDto FromAccount(Account account)
		{
			return new AccountResponseDto
			{
				Id = account.Id,
				Type = account.Type,
				Value = account.Value,
			};
		}
	}
}
