using EagleBank.Entities;

namespace EagleBank.Models
{
	public class UserResponseDto
	{
		public int Id { get; set; }
		public string Username { get; set;} = string.Empty;

		public static UserResponseDto FromUser(User user)
		{
			return new UserResponseDto
			{
				Id = user.Id,
				Username = user.Username,
			};
		}
	}
}
