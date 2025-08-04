using EagleBank.Models;
using EagleBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OneOf.Types;
using System.Security.Claims;

namespace EagleBank.Controllers
{
	[Route("v1/accounts")]
	[ApiController]
	public class AccountController(IAccountService accountService) : ControllerBase
	{
		[HttpPost]
		public async Task<ActionResult<AccountResponseDto>> CreateAccount(AccountDto request)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var response = await accountService.CreateAsync(request, nameId);

			if (response is null)
				return BadRequest(request);

			return Ok(response);
		}

		[HttpGet]
		public async Task<ActionResult<ICollection<AccountResponseDto>>> GetAccounts()
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var accounts = await accountService.GetAccountsAsync(nameId);
			
			if (accounts is null)
				return NotFound();
			
			return Ok(accounts);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetAccount(int id)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var result = await accountService.GetAccountAsync(nameId, id);

			var action = result.Match<IActionResult>(
				account => Ok(account),
				error => StatusCode((int)error.StatusCode, error.Message));

			return action;
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAccount(int id)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var result = await accountService.DeleteAccountAsync(nameId, id);

			var action = result.Match<IActionResult>(
				account => Ok(account),
				error => StatusCode((int)error.StatusCode, error.Message));

			return action;
		}
	}
}
