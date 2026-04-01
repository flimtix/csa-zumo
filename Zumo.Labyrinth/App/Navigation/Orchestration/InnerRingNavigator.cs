using Microsoft.Extensions.Logging;

namespace Zumo.Labyrinth.Navigation.Orchestration;

public sealed class InnerRingNavigator : IInnerRingNavigator
{
    private readonly NavigationStateContext stateContext;
    private readonly ILogger<InnerRingNavigator> logger;

    public InnerRingNavigator(
        NavigationStateContext stateContext,
        ILogger<InnerRingNavigator> logger)
    {
        this.stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunInnerRingNavigationAsync()
    {
        logger.LogInformation("Inner-ring navigation started.");
        await stateContext.RunAsync();
        logger.LogInformation("Inner-ring navigation finished.");
    }
}
