using EagleBank.Models;

namespace EagleBank.Services
{
	public interface IAuthService
	{
		Task<UserResponseDto?> CreateAsync(UserDto request);
		Task<LoginDto?> LoginAsync(UserDto request);
		Task<UserResponseDto?> FetchUserAsync(int id);
	}
}
