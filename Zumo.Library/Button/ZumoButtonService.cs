using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Button;

public sealed class ZumoButtonService : IZumoButtonService, IDisposable
{
    private const byte ButtonAddress = 0x61;
    private const byte ButtonSdf = 0x0;

    private readonly IEventRouter eventRouter;
    private readonly EventSubscriptionToken subscriptionToken;
    private readonly Lock stateLock = new();

    private bool isPressed;

    public ZumoButtonService(IEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
        subscriptionToken = eventRouter.Subscribe(new EventSubscription(ButtonAddress, MessageType.Event, ButtonSdf), OnEvent);
    }

    public bool IsPressed
    {
        get
        {
            bool value;
            using (stateLock.EnterScope())
            {
                value = isPressed;
            }

            return value;
        }
    }

    public event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;

    private void OnEvent(ProtocolFrame frame)
    {
        bool newPressed = ParsePressed(frame.Payload);
        bool raise;

        using (stateLock.EnterScope())
        {
            raise = isPressed != newPressed;
            isPressed = newPressed;
        }

        if (raise)
        {
            ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(newPressed));
        }
    }

    private static bool ParsePressed(string payload)
    {
        bool pressed = payload is "1" or "01";
        return pressed;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            eventRouter.Unsubscribe(subscriptionToken);
            ButtonChanged = null;
        }
    }
}
