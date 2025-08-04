using System.Net;

namespace EagleBank
{
	public class Error(HttpStatusCode statusCode, string message)
	{
		public HttpStatusCode StatusCode { get; } = statusCode;
		public string Message { get; } = message;
	}
}
