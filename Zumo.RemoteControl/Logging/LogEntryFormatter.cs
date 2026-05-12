using System.Globalization;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Execution;

namespace Zumo.RemoteControl.Logging;

internal static class LogEntryFormatter
{
    public static IEnumerable<string> Format(LogOptions options, RouteExecutionResult result)
    {
        var lines = new List<string>();

        var studentName = options.StudentName;
        lines.Add("// " + studentName);

        var timestamp = result.StartedAtUtc.UtcDateTime.ToString(options.TimestampFormat, CultureInfo.InvariantCulture);
        lines.Add("// " + timestamp);

        foreach (var line in result.UartResponses)
        {
            lines.Add(line);
        }

        if (result.Status == RouteExecutionStatus.StoppedByObstacle)
        {
            lines.Add(options.StopMarker);
        }

        return lines;
    }
}
