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
	public class AccountTests : DatabaseTests
	{
		private readonly HttpClient _httpClient;

		public AccountTests(CustomWebApplicationFactory factory) : base(factory)
		{
			var clientOptions = new WebApplicationFactoryClientOptions
			{
				AllowAutoRedirect = false
			};

			_httpClient = WebApplicationFactory.CreateClient(clientOptions);
		}

		[Fact]
		public async Task CreateAccount_ReturnsOkWithAccountResponseDto()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Login to get the JWT
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);

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
			response = await _httpClient.SendAsync(request);
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
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Login to get the JWT
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);

			int accountCount = 3;
			for (int i = 0; i < accountCount; i++)
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
				_ = await _httpClient.SendAsync(accountRequest);
			}

			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			response = await _httpClient.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var accountResponse = await response.Content.ReadFromJsonAsync<ICollection<AccountResponseDto>>();
			Assert.NotNull(accountResponse);
			Assert.Equal(accountCount, accountResponse.Count);
		}
	}
}
