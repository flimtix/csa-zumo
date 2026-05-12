using System.Globalization;
using System.Text;

namespace Zumo.RemoteControl.Servers;

public class HttpResponse
{
    public const string NewLine = "\r\n";

    public HttpResponse(string status, string message)
    {
        Status = status;
        Message = message;
    }

    public string Status { get; }
    public string Message { get; }

    public StringBuilder ToHttpResponse()
    {
        var length = Encoding.ASCII.GetByteCount(Message);
        var date = DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture);

        var responseBuilder = new StringBuilder()
            .Append("HTTP/1.1 ").Append(Status).Append(NewLine)
            .Append("Content-Type: text/plain; charset=us-ascii").Append(NewLine)
            .Append("Content-Length: ").Append(length.ToString()).Append(NewLine)
            .Append("Server: ZumoHttpServer/1.0").Append(NewLine)
            .Append("Date: ").Append(date).Append(NewLine)
            .Append("Connection: close").Append(NewLine)
            .Append(NewLine)
            .Append(Message);
        return responseBuilder;
    }
}