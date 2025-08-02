using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EagleBank.Tests
{
	[Collection(nameof(DatabaseTestCollection))]
	public class UserTests : IAsyncLifetime
	{
		private readonly WebApplicationFactory<Program> _webApplicationFactory;
		private readonly HttpClient _httpClient;
		private Func<Task> _resetDatabase;

		public UserTests(CustomWebApplicationFactory factory)
		{
			var clientOptions = new WebApplicationFactoryClientOptions
			{
				AllowAutoRedirect = false
			};

			_webApplicationFactory = factory;
			_httpClient = _webApplicationFactory.CreateClient(clientOptions);
			_resetDatabase = factory.ResetDatabase;
		}

		[Fact]
		public async Task CreateUser_WhenUsernameIsUnique_ReturnsOkWithUserDto()
		{
			// Arrange
			var userDto = new UserDto { Username = "testuser", Password = "password123" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var responseUser = await response.Content.ReadFromJsonAsync<UserDto>();
			Assert.NotNull(responseUser);
			Assert.Equal(userDto.Username, responseUser.Username);

			// Verify the user was added to the database
			using var scope = _webApplicationFactory.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<EagleBankDbContext>();
			var userInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == userDto.Username);
			Assert.NotNull(userInDb);
			Assert.Equal(userDto.Username, userInDb.Username);
		}

		[Fact]
		public async Task CreateUser_WhenUsernameExists_ReturnsBadRequest()
		{
			// Arrange
			var userDto = new UserDto { Username = "testuser", Password = "password123" };

			// Add a user to the database first
			using (var scope = _webApplicationFactory.Services.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<EagleBankDbContext>();
				dbContext.Users.Add(new User { Username = userDto.Username, PasswordHash = "hashedPassword" });
				await dbContext.SaveChangesAsync();
			}

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var responseContent = await response.Content.ReadAsStringAsync();
			Assert.Contains("Username already exists", responseContent);
		}

		public Task InitializeAsync() => Task.CompletedTask;

		public Task DisposeAsync() => _resetDatabase();
	}
}
