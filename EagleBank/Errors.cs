namespace EagleBank
{
	public record NotFoundError(int AccountId);
	public record ForbiddenError(int UserId, int AccountId);
	public record UnprocessableEntity(int UserId, int AccountId);
}
