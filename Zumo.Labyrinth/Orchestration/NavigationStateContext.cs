using Microsoft.Extensions.Logging;

namespace Zumo.Labyrinth.Orchestration;

public sealed class NavigationStateContext
{
    private readonly IReadOnlyDictionary<NavigationState, INavigationState> states;
    private readonly ILogger<NavigationStateContext> logger;
    private bool isSolved;

    public NavigationStateContext(
        IEnumerable<INavigationState> states,
        ILogger<NavigationStateContext> logger)
    {
        ArgumentNullException.ThrowIfNull(states);

        this.states = states.ToDictionary(state => state.State);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CurrentState = NavigationState.MoveToInnerRing;
        EnsureStateCoverage();
    }

    public NavigationState CurrentState { get; private set; }

    public async Task RunAsync()
    {
        logger.LogInformation("Navigation state machine started with {State}.", CurrentState);

        while (!isSolved)
        {
            if (!states.TryGetValue(CurrentState, out var handler))
            {
                throw new InvalidOperationException($"No state handler registered for state {CurrentState}.");
            }

            logger.LogTrace("Executing state {State}.", CurrentState);
            await handler.ExecuteAsync(this);
            await Task.Delay(200);
        }

        logger.LogInformation("Navigation state machine finished with {State}.", CurrentState);
    }

    public void TransitionTo(NavigationState nextState)
    {
        var previousState = CurrentState;
        CurrentState = nextState;

        if (previousState != nextState)
        {
            logger.LogInformation("State transition: {PreviousState} -> {NextState}", previousState, nextState);
        }
    }

    public void MarkSolved()
    {
        CurrentState = NavigationState.InnerRingSolved;
        isSolved = true;
    }

    private void EnsureStateCoverage()
    {
        var requiredStates = Enum.GetValues<NavigationState>();

        foreach (var requiredState in requiredStates)
        {
            if (!states.ContainsKey(requiredState))
            {
                throw new InvalidOperationException($"No handler registered for required state {requiredState}.");
            }
        }
    }
}
