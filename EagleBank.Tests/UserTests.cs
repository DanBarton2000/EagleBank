using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace EagleBank.Tests
{
	[Collection(nameof(DatabaseTestCollection))]
	public class UserTests : IAsyncLifetime
	{
		private readonly WebApplicationFactory<Program> _webApplicationFactory;
		private readonly HttpClient _httpClient;
		private readonly Func<Task> _resetDatabase;

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
			Assert.Contains("Failed to create user.", responseContent);
		}

		[Fact]
		public async Task CreateUser_MissingUsername_ReturnsBadRequest()
		{
			// Arrange
			var userDto = new { Password = "testuser" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreateUser_MissingPassword_ReturnsBadRequest()
		{
			// Arrange
			var userDto = new { Username = "testuser" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CreateUser_Null_ReturnsBadRequest()
		{
			// Arrange
			UserDto? userDto = null;

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task Login_WhenDetailsAreValid_ReturnsOkWithJWT()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var responseContent = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(responseContent);

			var configuration = _webApplicationFactory.Services.GetRequiredService<IConfiguration>();
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidIssuer = configuration["AppSettings:Issuer"],
				ValidateAudience = true,
				ValidAudience = configuration["AppSettings:Audience"],
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(configuration["AppSettings:Token"]!))
			};

			var tokenHandler = new JwtSecurityTokenHandler();

			try
			{
				var principal = tokenHandler.ValidateToken(responseContent.Token, tokenValidationParameters, out var validatedToken);

				Assert.NotNull(principal);
				Assert.IsType<JwtSecurityToken>(validatedToken);
				Assert.True(validatedToken.ValidTo > DateTime.UtcNow);
			}
			catch (SecurityTokenException ex)
			{
				Assert.Fail($"Token validation failed: {ex.Message}");
			}
		}

		[Fact]
		public async Task Login_WhenUserDoesntExist_ReturnsBadRequest()
		{
			// Arrange
			var userDto = new UserDto { Username = "testuser", Password = "password123" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var responseContent = await response.Content.ReadAsStringAsync();
			Assert.Contains("Invalid username or password.", responseContent);
		}

		[Fact]
		public async Task Login_WhenPasswordIsIncorrect_ReturnsBadRequest()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Arrange
			userDto = new UserDto { Username = "testuser", Password = "password1234" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var responseContent = await response.Content.ReadAsStringAsync();
			Assert.Contains("Invalid username or password.", responseContent);
		}

		[Fact]
		public async Task Login_WhenUsernameIsIncorrect_ReturnsBadRequest()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Arrange
			userDto = new UserDto { Username = "usertest", Password = "password123" };

			// Act
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var responseContent = await response.Content.ReadAsStringAsync();
			Assert.Contains("Invalid username or password.", responseContent);
		}

		[Fact]
		public async Task FetchUser_WhenIdIsValid_ReturnsOkWithUserResponseDto()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Login to get the JWT
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{loginDto.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			response = await _httpClient.SendAsync(request);

			// Assert
			var userResponse = await response.Content.ReadFromJsonAsync<UserResponseDto>();
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(userResponse);
			Assert.Equal(userDto.Username, userResponse.Username);
		}

		[Fact]
		public async Task FetchUser_WhenIdDoesntExist_ReturnsNotFound()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			// Login to get the JWT
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{loginDto.Id + 1}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			response = await _httpClient.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task FetchUser_FetchingAnthorUser_ReturnsForbidden()
		{
			// Add a user to the database first
			var userDto = new UserDto { Username = "testuser", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto);

			var userDto1 = new UserDto { Username = "testuser1", Password = "password123" };
			_ = await _httpClient.PostAsJsonAsync("/v1/users", userDto1);

			// Login to get the JWT
			var response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto);
			LoginDto? loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto);

			// Login with the other use to get the id
			response = await _httpClient.PostAsJsonAsync("/v1/users/login", userDto1);
			LoginDto? loginDto1 = await response.Content.ReadFromJsonAsync<LoginDto>();
			Assert.NotNull(loginDto1);

			// Act
			var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{loginDto1.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
			response = await _httpClient.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		public Task InitializeAsync() => Task.CompletedTask;

		public Task DisposeAsync() => _resetDatabase();
	}
}
