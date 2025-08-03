using Docker.DotNet.Models;
using EagleBank.Data;
using EagleBank.Models;
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
	public class TransactionTests(CustomWebApplicationFactory factory) : DatabaseTests(factory)
	{
		[Fact]
		public async Task CreateTransaction_ValidDepositTransaction_ReturnsOkWithTransactionDto()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);

			CreateTransactionDto createTransactionDto = new()
			{ 
				Amount = 10, 
				Type = Entities.TransactionType.Deposit 
			};

			var json = JsonSerializer.Serialize(createTransactionDto);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/{accountDto.Id}/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var transactionResponse = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
			Assert.NotNull(transactionResponse);
			Assert.Equal(accountDto.Id, transactionResponse.AccountId);
			Assert.Equal(createTransactionDto.Amount, transactionResponse.Amount);
			Assert.Equal(createTransactionDto.Type, transactionResponse.Type);

			// Verify the transaction was added to the database
			using var scope = WebApplicationFactory.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<EagleBankDbContext>();
			var transactionInDb = await dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == transactionResponse.Id);
			Assert.NotNull(transactionInDb);

			// Verify
			AccountResponseDto accountResponse = await GetAccount(loginDto, accountDto);
			Assert.Equal(accountResponse.Id, accountDto.Id);
			Assert.Equal(accountResponse.Value, createTransactionDto.Amount);
		}

		[Fact]
		public async Task CreateTransaction_InvalidWithdrawal_ReturnsUnprocessableEntity()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);

			CreateTransactionDto createTransactionDto = new()
			{
				Amount = 10,
				Type = Entities.TransactionType.Withdrawal
			};

			var json = JsonSerializer.Serialize(createTransactionDto);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/{accountDto.Id}/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		}

		[Fact]
		public async Task CreateTransaction_ValidWithdrawal_ReturnsOkWithTransactionResponseDto()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);
			TransactionResponseDto deposit = await CreateTransaction(loginDto, accountDto, 30, Entities.TransactionType.Deposit);

			CreateTransactionDto withdrawalTransaction = new()
			{
				Amount = 10,
				Type = Entities.TransactionType.Withdrawal
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/{accountDto.Id}/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			// Verify
			AccountResponseDto accountResponse = await GetAccount(loginDto, accountDto);
			Assert.Equal(accountResponse.Id, accountDto.Id);
			Assert.Equal(accountResponse.Value, deposit.Amount - withdrawalTransaction.Amount);
		}
	}
}
