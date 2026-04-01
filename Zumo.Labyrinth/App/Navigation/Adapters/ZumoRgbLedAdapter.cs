using System.Drawing;
using Zumo.Library.Led;

namespace Zumo.Labyrinth.Navigation.Adapters;

public sealed class ZumoRgbLedAdapter : ILedService
{
    private const byte AllLedsMask = 0xFF;
    private readonly IZumoRgbLedService rgbLedService;

    public ZumoRgbLedAdapter(IZumoRgbLedService rgbLedService)
    {
        this.rgbLedService = rgbLedService ?? throw new ArgumentNullException(nameof(rgbLedService));
    }

    public ValueTask SetStatusAsync(NavigationActivityStatus status)
    {
        var mapped = status switch
        {
            NavigationActivityStatus.Idle => Color.DimGray,
            NavigationActivityStatus.Scanning => Color.Blue,
            NavigationActivityStatus.DrivingStraight => Color.Yellow,
            NavigationActivityStatus.Turning => Color.MediumPurple,
            NavigationActivityStatus.Blocked => Color.Red,
            NavigationActivityStatus.Solved => Color.LimeGreen,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported navigation activity status."),
        };

        rgbLedService.SetTopColor(AllLedsMask, mapped);
        return ValueTask.CompletedTask;
    }
}
