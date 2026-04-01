namespace Zumo.Labyrinth.Navigation.Contracts;

public interface ILedService
{
    ValueTask SetStatusAsync(NavigationActivityStatus status);
}
