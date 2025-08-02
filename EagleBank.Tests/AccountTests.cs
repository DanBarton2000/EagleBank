using EagleBank.Data;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EagleBank.Tests
{
	public class AccountTests(CustomWebApplicationFactory factory) : DatabaseTests(factory)
	{
		[Fact]
		public async Task CreateAccount_ReturnsOkWithAccountResponseDto()
		{
			LoginDto loginDto = await CreateAndLoginUser("testuser", "password123");

			// Arrange
			AccountDto accountDto = new()
			{
				Type = Entities.AccountType.Current
			};

			var json = JsonSerializer.Serialize(accountDto);

			var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);
			Assert.NotNull(response);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
			Assert.NotNull(accountResponse);
			Assert.Equal(accountDto.Type, accountResponse.Type);
			Assert.Equal(0m, accountResponse.Value);

			// Verify the user was added to the database
			using var scope = WebApplicationFactory.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<EagleBankDbContext>();
			var accountInDb = await dbContext.Accounts.FirstOrDefaultAsync(account => account.Id == accountResponse.Id);
			Assert.NotNull(accountInDb);
			Assert.Equal(loginDto.Id, accountInDb.UserId);
		}


		[Fact]
		public async Task GetAccounts_ReturnsOkWithListAccountResponseDto()
		{
			// Add a user to the database first
			LoginDto loginDto = await CreateAndLoginUser("testuser", "password123");

			int accountCount = 3;
			for (int i = 0; i < accountCount; i++)
			{
				_ = await CreateCurrentAccount(loginDto);
			}

			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var accountResponse = await response.Content.ReadFromJsonAsync<ICollection<AccountResponseDto>>();
			Assert.NotNull(accountResponse);
			Assert.Equal(accountCount, accountResponse.Count);
		}

		[Fact]
		public async Task GetAccount_FetchOwnAccount_ReturnsOkWithAccountResponseDto()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("testuser", "password123");
			var accountResponse = await CreateCurrentAccount(loginDto);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountResponse.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);
			
			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var fetchedAccount = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
			Assert.NotNull(fetchedAccount);
			Assert.Equal(accountResponse.Id, fetchedAccount.Id);
			Assert.Equal(accountResponse.Type, fetchedAccount.Type);
			Assert.Equal(accountResponse.Value, fetchedAccount.Value);
		}

		[Fact]
		public async Task GetAccount_FetchAnotherUsersAccount_ReturnsForbidden()
		{
			// Arrange
			LoginDto user1 = await CreateAndLoginUser("testuser1", "password123");
			LoginDto user2 = await CreateAndLoginUser("testuser2", "password123");
			AccountResponseDto accountResponse = await CreateCurrentAccount(user1);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountResponse.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetAccount_FetchNonExistentAccount_ReturnsNotFound()
		{
			// Arrange
			LoginDto user1 = await CreateAndLoginUser("testuser1", "password123");

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{0}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}
	}
}
