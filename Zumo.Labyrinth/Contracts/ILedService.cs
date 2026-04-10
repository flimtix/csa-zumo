namespace Zumo.Labyrinth.Contracts;

public interface ILedService
{
    ValueTask SetStatusAsync(NavigationActivityStatus status);
}
