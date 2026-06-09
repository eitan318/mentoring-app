using System.Net;

namespace MentoringApp.ApiClient.Exceptions;

/// <summary>Thrown by the API clients when the server returns a non-success HTTP status; carries that <see cref="StatusCode"/>.</summary>
public class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
