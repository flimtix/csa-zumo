namespace Zumo.Labyrinth.States;

public sealed class InnerRingSolvedState : INavigationState
{
    private readonly ILedService ledService;

    public InnerRingSolvedState(ILedService ledService)
    {
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
    }

    public NavigationState State => NavigationState.InnerRingSolved;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Solved);
        context.MarkSolved();
    }
}
