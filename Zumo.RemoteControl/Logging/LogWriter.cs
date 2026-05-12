using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Execution;

namespace Zumo.RemoteControl.Logging;

public sealed class LogWriter : ILogWriter
{
    private readonly IOptions<RemoteControlOptions> options;

    public LogWriter(IOptions<RemoteControlOptions> options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> WriteAsync(RouteExecutionResult result, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var logOptions = options.Value.Log;
        var directory = logOptions.Directory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = "./logs";
        }

        Directory.CreateDirectory(directory);

        var timestamp = result.StartedAtUtc.UtcDateTime.ToString(logOptions.FileTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = logOptions.FileNameFormat
            .Replace("{route}", result.RouteCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", timestamp, StringComparison.OrdinalIgnoreCase);

        var path = Path.Combine(directory, fileName);
        var lines = LogEntryFormatter.Format(logOptions, result);

        await File.WriteAllLinesAsync(path, lines, Encoding.ASCII, ct);
        return path;
    }
}
