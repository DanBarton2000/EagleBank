using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EagleBank.Services
{
	public class AuthService(EagleBankDbContext context, IConfiguration configuration) : IAuthService
	{
		public async Task<UserDto?> CreateAsync(UserDto request)
		{
			if (string.IsNullOrEmpty(request.Username))
				return null;

			if (string.IsNullOrEmpty(request.Password))
				return null;

			if (await context.Users.AnyAsync(u => u.Username == request.Username)) 
				return null;

			var user = new User();
			var hashedPasword = new PasswordHasher<User>().HashPassword(user, request.Password);

			user.Username = request.Username;
			user.PasswordHash = hashedPasword;

			context.Users.Add(user);
			await context.SaveChangesAsync();

			return new UserDto
			{
				Username = request.Username,
			};
		}

		public async Task<string?> LoginAsync(UserDto request)
		{
			User? user = await context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

			if (user == null)
				return null;

			if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
				return null;

			return CreateToken(user);
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
