namespace Zumo.Labyrinth.Contracts;

public interface INavigationState
{
    NavigationState State { get; }
    Task ExecuteAsync(NavigationStateContext context);
}
