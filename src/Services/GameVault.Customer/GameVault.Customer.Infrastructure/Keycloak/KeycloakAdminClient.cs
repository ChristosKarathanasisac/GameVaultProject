using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Options;

namespace GameVault.Customer.Infrastructure.Keycloak;

public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakAdminClient(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            return CustomerErrors.EmailConflict;

        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location!.ToString();
        var userId = Guid.Parse(location.Split('/').Last());

        return userId;
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.AdminClientId,
            ["client_secret"] = _options.AdminClientSecret
        });

        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("access_token").GetString()!;
    }
}
