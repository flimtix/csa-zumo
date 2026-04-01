namespace Zumo.Labyrinth.Navigation.Contracts;

public interface INavigationState
{
    NavigationState State { get; }
    Task ExecuteAsync(NavigationStateContext context);
}
