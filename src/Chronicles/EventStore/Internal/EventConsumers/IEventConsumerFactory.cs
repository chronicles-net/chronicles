namespace Chronicles.EventStore.Internal.EventConsumers;

internal interface IEventConsumerFactory
{
    IEventConsumer CreateConsumer(string name);
}