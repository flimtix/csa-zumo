using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Execution;
using Zumo.RemoteControl.Logging;

namespace Zumo.RemoteControl.Test;

[TestClass]
public sealed class LogEntryFormatterTests
{
    [TestMethod]
    public void Format_WritesHeaderPingAndStopMarker()
    {
        var options = new LogOptions
        {
            StudentName = "Test User",
            TimestampFormat = "u",
            StopMarker = "STOP"
        };

        var startedAt = new DateTimeOffset(2026, 5, 4, 11, 2, 39, TimeSpan.Zero);
        var result = new RouteExecutionResult(
            "A",
            startedAt,
            RouteExecutionStatus.StoppedByObstacle,
            [
                "5<D1Ping",
                "5<241234"
            ]);

        var lines = LogEntryFormatter.Format(options, result).ToArray();

        Assert.HasCount(5, lines);
        Assert.AreEqual("// Test User", lines[0]);
        Assert.AreEqual("// 2026-05-04 11:02:39Z", lines[1]);
        Assert.AreEqual("5<D1Ping", lines[2]);
        Assert.AreEqual("5<241234", lines[3]);
        Assert.AreEqual("STOP", lines[4]);
    }
}
