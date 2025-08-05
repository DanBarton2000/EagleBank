using EagleBank.Entities;
using EagleBank.Models;
using EagleBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf.Types;
using System.Security.Claims;

namespace EagleBank.Controllers
{
	[Route("v1/users")]
	[ApiController]
	public class UserController(IUserService userService) : ControllerBase
	{
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> CreateUser(UserDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await userService.CreateAsync(request);

			var action = result.Match<IActionResult>(
							user => CreatedAtAction(nameof(FetchDetails), new { id = user.Id }, user),
							error => StatusCode((int)error.StatusCode, error.Message));

			return action;
		}

		[HttpPost]
		[Route("login")]
		[AllowAnonymous]
		public async Task<ActionResult<LoginDto>> Login(UserDto userDto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			LoginDto? loginDto = await userService.LoginAsync(userDto);

			if (loginDto == null)
				return BadRequest("Invalid username or password.");

			return CreatedAtAction(nameof(FetchDetails), new { id = loginDto.Id }, loginDto);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<UserResponseDto>> FetchDetails(int id)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");
			
			var response = await userService.FetchUserAsync(id);

			if (response is null)
				return NotFound();

			// Scenario: User wants to fetch the user details of a non-existent user
			// We have to call FetchUserAsync first to get the NotFound response
			if (nameId != id)
				return Forbid();

			return Ok(response);
		}
	}
}
