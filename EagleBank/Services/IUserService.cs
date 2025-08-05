using EagleBank.Models;
using OneOf;

namespace EagleBank.Services
{
	public interface IUserService
	{
		Task<OneOf<UserResponseDto, Error>> CreateAsync(UserDto request);
		Task<LoginDto?> LoginAsync(UserDto request);
		Task<UserResponseDto?> FetchUserAsync(int id);
	}
}
