namespace Zumo.Library.Ping;

public interface IPingService
{
    bool Ping(CancellationToken cancellationToken);
}
