using EagleBank.Models;
using EagleBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EagleBank.Controllers
{
	[Route("v1/users")]
	[ApiController]
	public class UserController(IAuthService authService) : ControllerBase
	{
		[HttpPost]
		[AllowAnonymous]
		public async Task<ActionResult<UserResponseDto>> CreateUser(UserDto request)
		{
			UserResponseDto? user = await authService.CreateAsync(request);

			if (user is null)
				return BadRequest("Failed to create user.");

			return Ok(user);
		}

		[HttpPost]
		[Route("login")]
		[AllowAnonymous]
		public async Task<ActionResult<LoginDto>> Login(UserDto userDto)
		{
			LoginDto? loginDto = await authService.LoginAsync(userDto);

			if (loginDto == null)
				return BadRequest("Invalid username or password.");

			return Ok(loginDto);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<UserResponseDto>> FetchDetails(int id)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");
			
			var response = await authService.FetchUserAsync(id);

			if (response is null)
				return NotFound();

			if (nameId != id)
				return Forbid();

			return Ok(response);
		}
	}
}
