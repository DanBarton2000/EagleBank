using EagleBank.Models;
using EagleBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace EagleBank.Controllers
{
	[Route("v1/users")]
	[ApiController]
	public class UserController(IAuthService authService) : ControllerBase
	{
		[HttpPost]
		public async Task<ActionResult<UserDto>> CreateUser(UserDto request)
		{
			UserDto? user = await authService.CreateAsync(request);

			if (user is null)
				return BadRequest("Failed to create user.");

			return Ok(user);
		}

		[HttpPost]
		[Route("login")]
		public async Task<ActionResult<string>> Login(UserDto userDto)
		{
			var token = await authService.LoginAsync(userDto);

			if (token == null)
				return BadRequest("Invalid username or password.");

			return Ok(token);
		}
	}
}
