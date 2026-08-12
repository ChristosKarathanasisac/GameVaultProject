using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapr.Client;
using GameVault.Core.Dapr;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameVault.Customer.Infrastructure.Keycloak;

public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;
    private readonly DaprClient _daprClient;
    private readonly IMemoryCache _cache;

    private const string AdminClientIdKey = "keycloak-admin-client-id";
    private const string AdminClientSecretKey = "keycloak-admin-client-secret";
    private const string AdminTokenCacheKey = "keycloak-admin-token";
    private const string DefaultRejectionMessage = "Keycloak rejected the registration request.";
    private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromSeconds(50);

    public KeycloakAdminClient(HttpClient httpClient, IOptions<KeycloakOptions> options, DaprClient daprClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _daprClient = daprClient;
        _cache = cache;
    }

    public async Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var response = await SendWithRetryAsync(token =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new
            {
                username = email,
                email,
                firstName,
                lastName,
                enabled = true,
                credentials = new[] { new { type = "password", value = password, temporary = false } }
            });
            return req;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            return CustomerErrors.EmailConflict;

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var detail = await ReadKeycloakErrorMessageAsync(response, cancellationToken);
            return CustomerErrors.RegistrationRejectedByKeycloak(detail);
        }

        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location!.ToString();
        var userId = Guid.Parse(location.Split('/').Last());

        return userId;
    }

    private static async Task<string> ReadKeycloakErrorMessageAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("errorMessage", out var errorMessage))
                return errorMessage.GetString() ?? DefaultRejectionMessage;
        }
        catch (JsonException)
        {
        }

        return DefaultRejectionMessage;
    }

    public async Task<Result<Unit>> ReactivateUserAsync(
        Guid userId,
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var response = await SendWithRetryAsync(token =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new
            {
                id = userId,
                username = email,
                email,
                firstName,
                lastName,
                enabled = true,
                credentials = new[] { new { type = "password", value = password, temporary = false } }
            });
            return req;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            return CustomerErrors.ReactivationConflict;

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var detail = await ReadKeycloakErrorMessageAsync(response, cancellationToken);
            return CustomerErrors.RegistrationRejectedByKeycloak(detail);
        }

        response.EnsureSuccessStatusCode();

        return Unit.Value;
    }

    public async Task<Result<Unit>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRetryAsync(
            token =>
            {
                var req = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"/admin/realms/{_options.Realm}/users/{userId}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return req;
            },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Unit.Value;

        if (!response.IsSuccessStatusCode)
            return CustomerErrors.DeleteRejectedByKeycloak;

        return Unit.Value;
    }

    // Sends a request and retries once with a fresh token if Keycloak responds 401 (expired token).
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<string, HttpRequestMessage> buildRequest,
        CancellationToken cancellationToken)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var response = await _httpClient.SendAsync(buildRequest(token), cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        _cache.Remove(AdminTokenCacheKey);
        token = await GetAdminTokenAsync(cancellationToken);
        return await _httpClient.SendAsync(buildRequest(token), cancellationToken);
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(AdminTokenCacheKey, out string? cached))
            return cached!;

        var clientIdTask = _daprClient.GetSecretAsync(
            DaprConsts.SecretStoreName, AdminClientIdKey, cancellationToken: cancellationToken);

        var clientSecretTask = _daprClient.GetSecretAsync(
            DaprConsts.SecretStoreName, AdminClientSecretKey, cancellationToken: cancellationToken);

        await Task.WhenAll(clientIdTask, clientSecretTask);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientIdTask.Result[AdminClientIdKey],
            ["client_secret"] = clientSecretTask.Result[AdminClientSecretKey]
        });

        var response = await _httpClient.PostAsync(
            $"/realms/{_options.Realm}/protocol/openid-connect/token",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var token = json.GetProperty("access_token").GetString()!;

        _cache.Set(AdminTokenCacheKey, token, TokenCacheDuration);

        return token;
    }
}
