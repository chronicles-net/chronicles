namespace Chronicles.EventStore;

public interface IConsumeEventStateProvider<out TState>
{
    TState Create(StreamEvent evt);
}