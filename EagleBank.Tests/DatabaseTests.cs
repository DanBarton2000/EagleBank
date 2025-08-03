using Azure;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

		public async Task<TransactionResponseDto> CreateTransaction(LoginDto loginDto, AccountResponseDto accountDto, decimal amount, TransactionType type)
		{
			CreateTransactionDto createTransactionDto = new()
			{
				Amount = amount,
				Type = type
			};

			var json = JsonSerializer.Serialize(createTransactionDto);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/{accountDto.Id}/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			var response = await Client.SendAsync(request);
			var transaction = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
			Assert.NotNull(transaction);
			return transaction;
		}

		public async Task<AccountResponseDto> GetAccount(LoginDto loginDto, AccountResponseDto accountResponseDto)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountResponseDto.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var fetchedAccount = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
			Assert.NotNull(fetchedAccount);
			return fetchedAccount;
		}
	}
}
