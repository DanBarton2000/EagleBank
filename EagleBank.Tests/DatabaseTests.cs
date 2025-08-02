using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EagleBank.Tests
{
	[Collection(nameof(DatabaseTestCollection))]
	public class DatabaseTests(CustomWebApplicationFactory factory) : IAsyncLifetime
	{
		protected readonly WebApplicationFactory<Program> WebApplicationFactory = factory;
		private readonly Func<Task> _resetDatabase = factory.ResetDatabase;

		public Task InitializeAsync() => Task.CompletedTask;
		public Task DisposeAsync() => _resetDatabase();
	}
}
