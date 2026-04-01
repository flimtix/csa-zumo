namespace Zumo.Library.Ping;

public interface IPingService
{
    bool DoPing(CancellationToken cancellationToken);
}
