using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Utils;

namespace Zumo.RemoteControl.Servers;

public sealed class HttpFileServer : BackgroundService
{
    private readonly ILogger<HttpFileServer> logger;
    private readonly IOptions<RemoteControlOptions> options;

    public HttpFileServer(ILogger<HttpFileServer> logger, IOptions<RemoteControlOptions> options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serverOptions = options.Value.HttpServer;
        var address = IPAddressUtils.ResolveBindAddress(serverOptions.BindAddress);
        using var listener = new TcpListener(address, serverOptions.Port);
        listener.Start();

        logger.LogInformation("HTTP file server listening on {Address}:{Port}.", address, serverOptions.Port);

        using var registration = stoppingToken.Register(listener.Stop);
        using var semaphore = new SemaphoreSlim(serverOptions.MaxConcurrentDownloads);

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

            if (client is null)
            {
                continue;
            }

            await semaphore.WaitAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested)
            {
                client.Dispose();
            }
            else
            {
                StartClientThread(client, semaphore, stoppingToken);
            }
        }
    }

    private Thread StartClientThread(TcpClient client, SemaphoreSlim semaphore, CancellationToken ct)
    {
        var thread = new Thread(async () => await HandleClientOnThread(client, semaphore, ct))
        {
            IsBackground = true,
            Name = "ZumoHttpServer.Client"
        };

        thread.Start();
        return thread;
    }

    private async Task HandleClientOnThread(TcpClient client, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
                {
                    AutoFlush = true,
                    NewLine = HttpResponse.NewLine
                };

                var requestLine = await reader.ReadLineAsync(ct);
                var response = await BuildResponseAsync(requestLine, ct);

                await WriteResponseAsync(writer, response, ct);
            }
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "Request canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to queue HTTP client.");
        }
        finally
        {
            client.Dispose();
            semaphore.Release();
        }
    }

    private async Task<HttpResponse> BuildResponseAsync(string? requestLine, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return new HttpResponse("400 Bad Request", "Empty request.");
        }

        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return new HttpResponse("400 Bad Request", "Invalid request line.");
        }

        var method = parts[0];
        var target = parts[1];

        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponse("405 Method Not Allowed", "Only GET is supported.");
        }

        if (TryGetFileName(target, out var fileName))
        {
            return new HttpResponse("404 Not Found", "File not found.");
        }

        var logDirectory = options.Value.Log.Directory;
        var fullPath = Path.Combine(logDirectory, fileName);
        if (!File.Exists(fullPath) || Path.GetDirectoryName(fullPath) != logDirectory)
        {
            return new HttpResponse("404 Not Found", "File not found.");
        }

        var fileBytes = await File.ReadAllTextAsync(fullPath, ct);
        return new HttpResponse("200 OK", fileBytes);
    }


    private static async Task WriteResponseAsync(StreamWriter stream, HttpResponse response, CancellationToken ct)
    {
        var responseBuilder = response.ToHttpResponse();
        await stream.WriteLineAsync(responseBuilder, ct);
        await stream.FlushAsync(ct);
    }

    private static bool TryGetFileName(string target, out string fileName)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            fileName = string.Empty;
        }
        else
        {
            int queryIndex = target.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                target = target[..queryIndex];
            }

            fileName = Path.GetFileName(target);
        }
        return string.IsNullOrWhiteSpace(fileName);
    }
}
