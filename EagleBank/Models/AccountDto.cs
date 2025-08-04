using EagleBank.Entities;
using System.ComponentModel.DataAnnotations;

namespace EagleBank.Models
{
	public class AccountDto
	{
		[Required]
		public AccountType? Type { get; set; }
	}
}
