using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;

namespace EagleBank.Tests
{
	public sealed class MsSqlTests : IAsyncLifetime
	{
		public readonly MsSqlContainer Container = new MsSqlBuilder().Build();

		public Task InitializeAsync()
		{
			return Container.StartAsync();
		}

		public Task DisposeAsync()
		{
			return Container.DisposeAsync().AsTask();
		}
	}
}
