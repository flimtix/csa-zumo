using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Execution;
using Zumo.RemoteControl.Logging;
using Zumo.RemoteControl.Routes;
using Zumo.RemoteControl.Utils;

namespace Zumo.RemoteControl.Servers;

public sealed class TcpControlServer : BackgroundService
{
    private readonly IRouteCatalog routeCatalog;
    private readonly IRouteExecutor routeExecutor;
    private readonly ILogWriter logWriter;
    private readonly ILogger<TcpControlServer> logger;
    private readonly IOptions<RemoteControlOptions> options;

    public TcpControlServer(
        IRouteCatalog routeCatalog,
        IRouteExecutor routeExecutor,
        ILogWriter logWriter,
        ILogger<TcpControlServer> logger,
        IOptions<RemoteControlOptions> options)
    {
        this.routeCatalog = routeCatalog ?? throw new ArgumentNullException(nameof(routeCatalog));
        this.routeExecutor = routeExecutor ?? throw new ArgumentNullException(nameof(routeExecutor));
        this.logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serverOptions = options.Value.TcpServer;
        var address = IPAddressUtils.ResolveBindAddress(serverOptions.BindAddress);
        using var listener = new TcpListener(address, serverOptions.Port);
        listener.Start();

        logger.LogInformation("TCP control server listening on {Address}:{Port}.", address, serverOptions.Port);

        using var registration = stoppingToken.Register(listener.Stop);

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync(stoppingToken);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (client is not null)
            {
                using (client)
                {
                    await HandleClientAsync(client, stoppingToken);
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
        {
            AutoFlush = true,
        };

        var route = await ReadRouteSelectionAsync(reader, writer, stoppingToken);
        if (route != null && !stoppingToken.IsCancellationRequested)
        {
            var result = await routeExecutor.ExecuteAsync(route, stoppingToken);
            var logPath = await logWriter.WriteAsync(result, stoppingToken);

            await writer.WriteLineAsync($"Route execution finished with status: {result.Status}. Log saved to: {logPath}");

            var downloadLink = BuildDownloadLink(logPath);
            await writer.WriteLineAsync(downloadLink);

            logger.LogInformation("Route {RouteCode} finished with status {Status}. Log: {LogPath}.", route.Code, result.Status, logPath);
        }
        await writer.WriteLineAsync("Closing connection. Goodbye!");
        await writer.FlushAsync(stoppingToken);

        // Send FIN to client and close connection as otherwise netcat does not terminate
        // and waits for server to close connection
        client.Client.Shutdown(SocketShutdown.Both);
        stream.Close();
        client.Close();
    }

    private async Task<RouteDefinition?> ReadRouteSelectionAsync(StreamReader reader, StreamWriter writer, CancellationToken ct)
    {
        var availableRoutes = string.Join(", ", routeCatalog.GetAllRoutes().Select(r => r.Code));
        await writer.WriteLineAsync($"Choose one of the following routes: {availableRoutes} and press Enter.");
        var input = await reader.ReadLineAsync(ct);

        RouteDefinition? route = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            await writer.WriteLineAsync("No route selected.");
        }
        else if (routeCatalog.TryGetRoute(input.Trim(), out var selectedRoute))
        {
            route = selectedRoute;
            await writer.WriteLineAsync($"Route {selectedRoute.Code} selected.");
        }
        else
        {
            await writer.WriteLineAsync($"Invalid route. Use one of the following: {availableRoutes}.");
        }
        return route;
    }

    private string BuildDownloadLink(string logPath)
    {
        var httpOptions = options.Value.HttpServer;
        var fileName = Path.GetFileName(logPath);
        var hostName = Dns.GetHostName();
        var url = $"http://{hostName}:{httpOptions.Port}/{fileName}";
        var message = "Download Log: " + url;
        return message;
    }
}
