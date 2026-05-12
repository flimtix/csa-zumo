using Microsoft.Extensions.Options;
using Zumo.Library;

namespace Zumo.RemoteControl.Runtime;

public interface IZumoRuntimeFactory
{
    ZumoRuntime Create();
}

public sealed class ZumoRuntimeFactory : IZumoRuntimeFactory
{
    private readonly IOptions<ZumoRuntimeOptions> options;

    public ZumoRuntimeFactory(IOptions<ZumoRuntimeOptions> options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ZumoRuntime Create()
    {
        return new ZumoRuntime(options.Value);
    }
}
