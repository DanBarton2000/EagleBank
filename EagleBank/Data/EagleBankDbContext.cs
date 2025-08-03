using EagleBank.Entities;
using Microsoft.EntityFrameworkCore;

namespace EagleBank.Data
{
	public class EagleBankDbContext(DbContextOptions<EagleBankDbContext> options) : DbContext(options)
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Account> Accounts { get; set; }
		public DbSet<Transaction> Transactions { get; set; }
	}
}
