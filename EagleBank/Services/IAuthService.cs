using EagleBank.Models;

namespace EagleBank.Services
{
	public interface IAuthService
	{
		Task<UserDto?> CreateAsync(UserDto request);
		Task<string?> LoginAsync(UserDto request);
	}
}
