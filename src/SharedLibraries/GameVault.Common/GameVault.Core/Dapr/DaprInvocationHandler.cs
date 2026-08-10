namespace GameVault.Core.Dapr;

public sealed class DaprInvocationHandler : DelegatingHandler
{
    private static readonly string DaprHttpPort =
        Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var appId = request.RequestUri!.Host;
        var pathAndQuery = request.RequestUri.PathAndQuery;

        request.Headers.TryAddWithoutValidation("dapr-app-id", appId);
        request.RequestUri = new Uri($"http://127.0.0.1:{DaprHttpPort}{pathAndQuery}");

        return base.SendAsync(request, cancellationToken);
    }
}
