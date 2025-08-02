using EagleBank.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using System.Data.Common;
using Testcontainers.MsSql;

// Mainly taken from: https://daninacan.com/resetting-your-test-database-in-c-with-respawn/

namespace EagleBank.Tests
{
	[CollectionDefinition(nameof(DatabaseTestCollection))]
	public class DatabaseTestCollection : ICollectionFixture<CustomWebApplicationFactory>
	{
	}

	public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
	{
		private readonly MsSqlContainer _container = new MsSqlBuilder().Build();
		private Respawner _respawner = null!;
		private DbConnection _connection = null!;

		public async Task ResetDatabase()
		{
			await _respawner.ResetAsync(_connection);
		}

		public async Task InitializeAsync()
		{
			await _container.StartAsync();

			var dbContext = Services.CreateScope().ServiceProvider.GetRequiredService<EagleBankDbContext>();
			_connection = dbContext.Database.GetDbConnection();
			await _connection.OpenAsync();

			_respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions());
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveDbContext<EagleBankDbContext>();
				services.AddDbContext<EagleBankDbContext>(options => options.UseSqlServer(_container.GetConnectionString()));
				services.EnsureDbCreated<EagleBankDbContext>();
			});
		}

		async Task IAsyncLifetime.DisposeAsync()
		{
			await _connection.CloseAsync();
			await _container.DisposeAsync();
		}
	}

	public static class ServiceCollectionExtensions
	{
		public static void RemoveDbContext<T>(this IServiceCollection services) where T : DbContext
		{
			var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<T>));
			if (descriptor != null)
			{
				services.Remove(descriptor);
			}
		}

		public static void EnsureDbCreated<T>(this IServiceCollection services) where T : DbContext
		{
			using var scope = services.BuildServiceProvider().CreateScope();
			var serviceProvider = scope.ServiceProvider;
			var context = serviceProvider.GetRequiredService<T>();
			context.Database.EnsureCreated();
		}
	}
}
