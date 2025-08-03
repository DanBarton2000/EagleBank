using EagleBank.Models;
using EagleBank.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EagleBank.Controllers
{
	[Route("v1/accounts/{accountId}/transactions")]
	[ApiController]
	public class TransactionsController(ITransactionService transactionService) : ControllerBase
	{
		[HttpPost]
		public async Task<IActionResult> CreateTransaction(int accountId, [FromBody] CreateTransactionDto dto)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var result = await transactionService.CreateTransaction(nameId, accountId, dto);

			var action = result.Match<IActionResult>(
							transaction => Ok(transaction),
							notFound => NotFound(),
							forbidden => Forbid(),
							unprocessableEntity => StatusCode(StatusCodes.Status422UnprocessableEntity));

			return action;
		}

		[HttpGet]
		public async Task<IActionResult> GetTransactions(int accountId)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var result = await transactionService.GetTransactions(nameId, accountId);

			var action = result.Match<IActionResult>(
							transaction => Ok(transaction),
							notFound => NotFound(),
							forbidden => Forbid());

			return action;
		}

		[HttpGet("{transactionId}")]
		public async Task<IActionResult> GetTransaction(int accountId, int transactionId)
		{
			Claim? nameIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

			if (nameIdClaim is null)
				return Forbid();

			if (!int.TryParse(nameIdClaim.Value, out int nameId))
				return BadRequest("JWT did not contain Id.");

			var result = await transactionService.GetTransaction(nameId, accountId, transactionId);

			var action = result.Match<IActionResult>(
							transaction => Ok(transaction),
							notFound => NotFound(),
							forbidden => Forbid());

			return action;
		}
	}
}
