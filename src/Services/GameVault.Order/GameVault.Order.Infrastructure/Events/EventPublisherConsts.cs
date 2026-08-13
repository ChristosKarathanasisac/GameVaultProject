namespace GameVault.Order.Infrastructure.Events;

internal static class EventPublisherConsts
{
    internal const string DaprHttpClientName = "dapr-pubsub";
    internal const string PubSubName = "gamevault-pubsub";
    internal const string OrderCompletedTopic = "order-completed";
}
