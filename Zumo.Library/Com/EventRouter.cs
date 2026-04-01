using System.Reflection;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public sealed class EventRouter : IEventRouter
{
    private readonly Lock syncLock = new();
    private readonly Dictionary<EventSubscriptionToken, WeakSubscription> subscriptions = [];

    public EventRouter() { }

    public EventSubscriptionToken Subscribe(EventSubscription subscription, ProtocolEventHandler handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        EventSubscriptionToken token = new EventSubscriptionToken(Guid.NewGuid());

        using (syncLock.EnterScope())
        {
            subscriptions[token] = WeakSubscription.Create(subscription, handler);
        }

        return token;
    }

    public bool Unsubscribe(EventSubscriptionToken token)
    {
        bool removed;
        using (syncLock.EnterScope())
        {
            removed = subscriptions.Remove(token);
        }

        return removed;
    }

    public void Route(ProtocolFrame frame)
    {
        List<(EventSubscriptionToken Token, WeakSubscription Subscription)> snapshot = [];

        using (syncLock.EnterScope())
        {
            foreach (KeyValuePair<EventSubscriptionToken, WeakSubscription> entry in subscriptions)
            {
                snapshot.Add((entry.Key, entry.Value));
            }
        }

        List<EventSubscriptionToken> deadTokens = [];

        foreach ((EventSubscriptionToken Token, WeakSubscription Subscription) item in snapshot)
        {
            bool isMatch = IsMatch(item.Subscription.Filter, frame);
            if (isMatch)
            {
                bool invoked;
                try
                {
                    invoked = item.Subscription.Invoke(frame);
                }
                catch
                {
                    invoked = true;
                }

                if (!invoked)
                {
                    deadTokens.Add(item.Token);
                }
            }
        }

        if (deadTokens.Count > 0)
        {
            using (syncLock.EnterScope())
            {
                foreach (EventSubscriptionToken token in deadTokens)
                {
                    subscriptions.Remove(token);
                }
            }
        }
    }

    public int CleanupDeadSubscriptions()
    {
        List<EventSubscriptionToken> dead = [];

        using (syncLock.EnterScope())
        {
            foreach (KeyValuePair<EventSubscriptionToken, WeakSubscription> entry in subscriptions)
            {
                if (!entry.Value.IsAlive)
                {
                    dead.Add(entry.Key);
                }
            }

            foreach (EventSubscriptionToken token in dead)
            {
                subscriptions.Remove(token);
            }
        }

        return dead.Count;
    }

    private static bool IsMatch(EventSubscription subscription, ProtocolFrame frame)
    {
        bool matchesAddress = !subscription.Address.HasValue || subscription.Address.Value == frame.Address;
        bool matchesType = subscription.Type == frame.Type;
        bool matchesSdf = subscription.Sdf == frame.Sdf;
        bool isMatch = matchesAddress && matchesType && matchesSdf;

        return isMatch;
    }

    private sealed class WeakSubscription
    {
        private readonly WeakReference<object>? targetReference;
        private readonly MethodInfo? methodInfo;
        private readonly ProtocolEventHandler? staticHandler;

        private WeakSubscription(EventSubscription filter, WeakReference<object>? targetReference, MethodInfo? methodInfo, ProtocolEventHandler? staticHandler)
        {
            Filter = filter;
            this.targetReference = targetReference;
            this.methodInfo = methodInfo;
            this.staticHandler = staticHandler;
        }

        public EventSubscription Filter { get; }

        public bool IsAlive
        {
            get
            {
                bool alive;

                if (staticHandler is not null)
                {
                    alive = true;
                }
                else if (targetReference is not null)
                {
                    alive = targetReference.TryGetTarget(out _);
                }
                else
                {
                    alive = false;
                }

                return alive;
            }
        }

        public static WeakSubscription Create(EventSubscription filter, ProtocolEventHandler handler)
        {
            WeakSubscription result;
            object? target = handler.Target;

            if (target is null)
            {
                result = new WeakSubscription(filter, null, null, handler);
            }
            else
            {
                result = new WeakSubscription(filter, new WeakReference<object>(target), handler.Method, null);
            }

            return result;
        }

        public bool Invoke(ProtocolFrame frame)
        {
            bool invoked = false;

            if (staticHandler is not null)
            {
                staticHandler(frame);
                invoked = true;
            }
            else if (targetReference is not null && methodInfo is not null)
            {
                bool hasTarget = targetReference.TryGetTarget(out object? target);
                if (hasTarget && target is not null)
                {
                    Delegate del = Delegate.CreateDelegate(typeof(ProtocolEventHandler), target, methodInfo);
                    ProtocolEventHandler dynamicHandler = (ProtocolEventHandler)del;
                    dynamicHandler(frame);
                    invoked = true;
                }
            }

            return invoked;
        }
    }
}
