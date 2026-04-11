using Microsoft.Extensions.Logging;

namespace Zumo.Labyrinth.States;

public sealed class LabyrinthSolvedState : INavigationState
{
    private readonly ILedService ledService;
    private readonly ILogger<LabyrinthSolvedState> logger;

    public LabyrinthSolvedState(
        ILedService ledService,
        ILogger<LabyrinthSolvedState> logger)
    {
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public NavigationState State => NavigationState.LabyrinthSolved;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogInformation("Labyrinth successfully exited. Navigation complete.");
        await ledService.SetStatusAsync(NavigationActivityStatus.Solved);
        context.MarkSolved(NavigationState.LabyrinthSolved);
    }
}
