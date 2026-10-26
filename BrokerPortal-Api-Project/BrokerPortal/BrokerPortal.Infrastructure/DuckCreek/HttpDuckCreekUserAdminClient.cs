using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BrokerPortal.Application.DuckCreek;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Infrastructure.DuckCreek;

/// <summary>
/// Calls the real Duck Creek User Admin API:
/// POST {BaseUrl}/ServerPolicy/api/v1/users?UserName=&amp;FullName=&amp;Password=&amp;FailIfExists=&amp;entityId=
/// with HTTP Basic authentication. Not exercised yet: there is no Duck Creek environment to test against,
/// so verify the request shape and responses against it before switching DuckCreek:Mode to "Http".
/// </summary>
public sealed class HttpDuckCreekUserAdminClient(
    HttpClient httpClient,
    IOptions<DuckCreekOptions> options) : IDuckCreekUserAdminClient
{
    private readonly DuckCreekOptions settings = options.Value;

    public async Task<DuckCreekUserResult> CreateUserAsync(
        DuckCreekCreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new DuckCreekUnavailableException(
                "Duck Creek is not configured. Set DuckCreek:BaseUrl, Username and Password.");
        }

        var query = new Dictionary<string, string?>
        {
            ["UserName"] = request.UserName,
            ["FullName"] = request.FullName,
            ["Password"] = request.Password,
            ["FailIfExists"] = request.FailIfExists ? "true" : "false",
            ["entityId"] = request.EntityId.ToString()
        };
        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));
        var url = $"{settings.BaseUrl.TrimEnd('/')}/ServerPolicy/api/v1/users?{queryString}";

        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            // The documented call also carries the same fields as a JSON body.
            Content = JsonContent.Create(new
            {
                userName = request.UserName,
                fullName = request.FullName,
                password = request.Password
            })
        };
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}")));

        HttpResponseMessage response;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
            response = await httpClient.SendAsync(message, timeout.Token);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new DuckCreekUnavailableException("Duck Creek could not be reached.", exception);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Conflict && request.FailIfExists)
            {
                throw new DuckCreekUserExistsException(request.UserName);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return new DuckCreekUserResult(request.UserName, request.EntityId, Created: false);
            }

            if (!response.IsSuccessStatusCode)
            {
                // The response body is deliberately not echoed: it may contain request details.
                throw new DuckCreekUnavailableException(
                    $"Duck Creek rejected the request with status {(int)response.StatusCode}.");
            }

            return new DuckCreekUserResult(request.UserName, request.EntityId, Created: true);
        }
    }
}
