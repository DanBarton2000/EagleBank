using EagleBank.Data;
using EagleBank.Entities;
using EagleBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EagleBank.Services
{
	public class AuthService(EagleBankDbContext context) : IAuthService
	{
		public async Task<UserDto?> CreateAsync(UserDto request)
		{
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
	}
}
