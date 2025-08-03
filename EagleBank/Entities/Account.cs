namespace EagleBank.Entities
{
	public enum AccountType
	{
		Current,
		Savings
	}

	public class Account
	{
		public int Id { get; set; }
		public AccountType Type { get; set; }
		public decimal Value { get; set; }

		public int UserId { get; set; }
		public User? User { get; set; }
	}
}
