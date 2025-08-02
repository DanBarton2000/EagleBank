using Azure;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EagleBank.Tests
{
	[Collection(nameof(DatabaseTestCollection))]
	public class DatabaseTests(CustomWebApplicationFactory factory) : IAsyncLifetime
	{
		protected readonly WebApplicationFactory<Program> WebApplicationFactory = factory;
		protected readonly HttpClient Client = factory.CreateClient();
		private readonly Func<Task> _resetDatabase = factory.ResetDatabase;

		public Task InitializeAsync() => Task.CompletedTask;
		public Task DisposeAsync() => _resetDatabase();
	
		public async Task<LoginDto> CreateAndLoginUser(string username, string password)
		{
			var userDto = new UserDto { Username = username, Password = password };
			_ = await Client.PostAsJsonAsync("/v1/users", userDto);

			var response = await Client.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);
			return loginDto;
		}

		public async Task<AccountResponseDto> CreateCurrentAccount(LoginDto loginDto)
		{
			AccountDto accountDto = new()
			{
				Type = Entities.AccountType.Current
			};

			var json = JsonSerializer.Serialize(accountDto);

			var accountRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			accountRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(accountRequest);
			var account = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
			Assert.NotNull(account);
			return account;
		}
	}
}
