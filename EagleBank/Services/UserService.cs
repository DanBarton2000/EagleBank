using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EagleBank.Services
{
	public class UserService(EagleBankDbContext context, IConfiguration configuration) : IUserService
	{
		public async Task<OneOf<UserResponseDto, Error>> CreateAsync(UserDto request)
		{
			if (string.IsNullOrEmpty(request.Username))
				return new Error(System.Net.HttpStatusCode.BadRequest, "Missing username.");

			if (string.IsNullOrEmpty(request.Password))
				return new Error(System.Net.HttpStatusCode.BadRequest, "Missing password.");

			if (await context.Users.AnyAsync(u => u.Username == request.Username))
				return new Error(System.Net.HttpStatusCode.Conflict, "User already exists.");

			var user = new User();
			var hashedPasword = new PasswordHasher<User>().HashPassword(user, request.Password);

			user.Username = request.Username;
			user.PasswordHash = hashedPasword;

			context.Users.Add(user);
			await context.SaveChangesAsync();

			return UserResponseDto.FromUser(user);
		}

		public async Task<LoginDto?> LoginAsync(UserDto request)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

			if (user == null)
				return null;

			if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
				return null;

			LoginDto loginDto = new()
			{
				Id = user.Id,
				Token = CreateToken(user)
			};

			return loginDto;
		}

		public async Task<UserResponseDto?> FetchUserAsync(int id)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Id == id);

			if (user == null)
				return null;

			return UserResponseDto.FromUser(user);
		}

		private string CreateToken(User user)
		{
			var claims = new List<Claim>
			{
				new(ClaimTypes.Name, user.Username),
				new(ClaimTypes.NameIdentifier, user.Id.ToString())
			};

			var key = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(configuration["AppSettings:Token"]!));

			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

			var tokenDescripter = new JwtSecurityToken(
				issuer: configuration["AppSettings:Issuer"],
				audience: configuration["AppSettings:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddDays(1),
				signingCredentials: creds
				);

			return new JwtSecurityTokenHandler().WriteToken(tokenDescripter);
		}
	}
}
