using Chronicles.Cqrs;

namespace Chronicles.EventStore.Samples;

public class QuestProjection
    : IDocumentProjection<QuestDocument>
{
    public QuestDocument CreateState(StreamId streamId)
        => new()
        {
            Id = streamId.Id,
            Pk = streamId.Id
        };

    public QuestDocument ConsumeEvent(
        StreamEvent evt,
        QuestDocument state)
        => evt.Data switch
        {
            QuestEvents.QuestStarted e => Consume(e, state),
            QuestEvents.MembersJoined e => Consume(e, state),
            _ => state,
        };

    private static QuestDocument Consume(
        QuestEvents.MembersJoined e,
        QuestDocument state)
    {
        state.Members = state.Members.Concat(e.Members).ToArray();

        return state;
    }

    private static QuestDocument Consume(
        QuestEvents.QuestStarted e,
        QuestDocument state)
    {
        state.Name = e.Name;

        return state;
    }
}