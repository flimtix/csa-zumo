using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public interface IEventRouter
{
    EventSubscriptionToken Subscribe(EventSubscription subscription, ProtocolEventHandler handler);

    bool Unsubscribe(EventSubscriptionToken token);

    void Route(ProtocolFrame frame);

    int CleanupDeadSubscriptions();
}
