using GameVault.Core.Correlation;
using Microsoft.AspNetCore.Http;

namespace GameVault.Core.Dapr;

public sealed class DaprInvocationHandler : DelegatingHandler
{
    private static readonly string DaprHttpPort =
        Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DaprInvocationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var appId = request.RequestUri!.Host;
        var pathAndQuery = request.RequestUri.PathAndQuery;

        request.Headers.TryAddWithoutValidation("dapr-app-id", appId);
        request.RequestUri = new Uri($"http://127.0.0.1:{DaprHttpPort}{pathAndQuery}");

        var correlationId = _httpContextAccessor.HttpContext?
            .Request.Headers[CorrelationIdConsts.HeaderName]
            .FirstOrDefault();

        if (correlationId is not null)
            request.Headers.TryAddWithoutValidation(CorrelationIdConsts.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
