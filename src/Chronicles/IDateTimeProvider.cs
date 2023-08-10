namespace Chronicles;

public interface IDateTimeProvider
{
    DateTimeOffset GetDateTime();
}

public class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetDateTime()
        => DateTimeOffset.UtcNow;
}
