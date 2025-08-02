using EagleBank.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace EagleBank.Tests
{
	internal sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
	{
		private readonly string _connectionString;

		public CustomWebApplicationFactory(MsSqlTests fixture)
		{
			_connectionString = fixture.Container.GetConnectionString();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.ConfigureServices(services =>
			{
				services.Remove(services.SingleOrDefault(service => typeof(DbContextOptions<EagleBankDbContext>) == service.ServiceType));
				services.Remove(services.SingleOrDefault(service => typeof(DbConnection) == service.ServiceType));
				services.AddDbContext<EagleBankDbContext>((_, option) => option.UseSqlServer(_connectionString));
			});
		}
	}
}
