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
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

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
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			// Verify
			AccountResponseDto accountResponse = await GetAccount(loginDto, accountDto);
			Assert.Equal(accountResponse.Id, accountDto.Id);
			Assert.Equal(accountResponse.Value, deposit.Amount - withdrawalTransaction.Amount);
		}

		[Fact]
		public async Task CreateTransaction_DepositAnotherUser_ReturnsForbidden()
		{
			// Arrange
			LoginDto loginDto1 = await CreateAndLoginUser("username1", "password123");
			LoginDto loginDto2 = await CreateAndLoginUser("username2", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto1);

			CreateTransactionDto withdrawalTransaction = new()
			{
				Amount = 10,
				Type = Entities.TransactionType.Deposit
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/{accountDto.Id}/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto2.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			// Verify
			AccountResponseDto accountResponse = await GetAccount(loginDto1, accountDto);
			Assert.Equal(accountResponse.Id, accountDto.Id);
			Assert.Equal(0, accountResponse.Value);
		}

		[Fact]
		public async Task CreateTransaction_WithdrawAnotherUser_ReturnsForbidden()
		{
			// Arrange
			LoginDto loginDto1 = await CreateAndLoginUser("username1", "password123");
			LoginDto loginDto2 = await CreateAndLoginUser("username2", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto1);

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
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto2.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			// Verify
			AccountResponseDto accountResponse = await GetAccount(loginDto1, accountDto);
			Assert.Equal(accountResponse.Id, accountDto.Id);
			Assert.Equal(0, accountResponse.Value);
		}

		[Fact]
		public async Task CreateTransaction_DepositAccountDoesntExist_ReturnsNotFound()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username1", "password123");

			CreateTransactionDto withdrawalTransaction = new()
			{
				Amount = 10,
				Type = Entities.TransactionType.Deposit
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/0/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task CreateTransaction_WithdrawalAccountDoesntExist_ReturnsNotFound()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username1", "password123");

			CreateTransactionDto withdrawalTransaction = new()
			{
				Amount = 10,
				Type = Entities.TransactionType.Withdrawal
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/0/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task CreateTransaction_MissingAmount_ReturnsBadRequest()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username1", "password123");

			var withdrawalTransaction = new
			{
				Type = Entities.TransactionType.Withdrawal
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/0/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreateTransaction_MissingType_ReturnsBadRequest()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username1", "password123");

			var withdrawalTransaction = new
			{
				Amount = 10
			};

			var json = JsonSerializer.Serialize(withdrawalTransaction);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/accounts/0/transactions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);

			// Act
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task GetTransactions_GetAll_ReturnsOkWithCollectionOfTransactionsResultDto()
		{
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);

			List<TransactionResponseDto> transactions = [];

			int transactionsCount = 4;
			for (int i = 0; i < transactionsCount; i++)
			{
				transactions.Add(await CreateTransaction(loginDto, accountDto, 10 + (i * 10), Entities.TransactionType.Deposit));
			}

			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountDto.Id}/transactions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			ICollection<TransactionResponseDto>? transactionsFromGet = await response.Content.ReadFromJsonAsync<ICollection<TransactionResponseDto>>();
			Assert.NotNull(transactionsFromGet);
			Assert.Equal(transactions.Count, transactionsFromGet.Count);

			for (int i = 0; i < transactionsCount; i++)
			{
				Assert.Equal(transactions[i].Amount, transactionsFromGet.ElementAt(i).Amount);
				Assert.Equal(transactions[i].Type, transactionsFromGet.ElementAt(i).Type);
				Assert.Equal(transactions[i].Id, transactionsFromGet.ElementAt(i).Id);
				Assert.Equal(transactions[i].AccountId, transactionsFromGet.ElementAt(i).AccountId);
			}
		}

		[Fact]
		public async Task GetTransactions_GetOtherUser_ReturnsForbidden()
		{
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			LoginDto loginDto2 = await CreateAndLoginUser("username2", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);

			List<TransactionResponseDto> transactions = [];

			int transactionsCount = 4;
			for (int i = 0; i < transactionsCount; i++)
			{
				transactions.Add(await CreateTransaction(loginDto, accountDto, 10 + (i * 10), Entities.TransactionType.Deposit));
			}

			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountDto.Id}/transactions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto2.Token);
			var response = await Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			// Make sure we aren't getting any data returned even though we got a Forbidden status code.
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<ICollection<TransactionResponseDto>>());
		}

		[Fact]
		public async Task GetTransactions_AccountDoesntExist_ReturnsNotfound()
		{
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");

			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/0/transactions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			// Make sure we aren't getting any data returned even though we got a NotFound status code.
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<ICollection<TransactionResponseDto>>());
		}

		[Fact]
		public async Task GetTransaction_ValidId_ReturnsOkWithResponseTransactionDto()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);
			TransactionResponseDto createTransaction = await CreateTransaction(loginDto, accountDto, 10, Entities.TransactionType.Deposit);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountDto.Id}/transactions/{createTransaction.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			TransactionResponseDto? transactionFromGet = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
			Assert.NotNull(transactionFromGet);
			Assert.Equal(createTransaction.Amount, transactionFromGet.Amount);
			Assert.Equal(createTransaction.Type, transactionFromGet.Type);
			Assert.Equal(createTransaction.Id, transactionFromGet.Id);
			Assert.Equal(createTransaction.AccountId, transactionFromGet.AccountId);
		}

		[Fact]
		public async Task GetTransaction_AnotherUsersAccount_ReturnsForbidden()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			LoginDto loginDto2 = await CreateAndLoginUser("username2", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);
			TransactionResponseDto createTransaction = await CreateTransaction(loginDto, accountDto, 10, Entities.TransactionType.Deposit);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountDto.Id}/transactions/{createTransaction.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto2.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<TransactionResponseDto>());
		}

		[Fact]
		public async Task GetTransaction_NonExistentAccount_ReturnsNotFound()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/1/transactions/1");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<TransactionResponseDto>());
		}

		[Fact]
		public async Task GetTransaction_NonExistentTransaction_ReturnsNotFound()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto accountDto = await CreateCurrentAccount(loginDto);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{accountDto.Id}/transactions/1");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<TransactionResponseDto>());
		}

		[Fact]
		public async Task GetTransaction_WrongAccount_ReturnsNotFound()
		{
			// Arrange
			LoginDto loginDto = await CreateAndLoginUser("username", "password123");
			AccountResponseDto account1 = await CreateCurrentAccount(loginDto);
			AccountResponseDto account2 = await CreateCurrentAccount(loginDto);
			TransactionResponseDto createTransaction = await CreateTransaction(loginDto, account1, 10, Entities.TransactionType.Deposit);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/accounts/{account2.Id}/transactions/{createTransaction.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			var response = await Client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<TransactionResponseDto>());
		}
	}
}
