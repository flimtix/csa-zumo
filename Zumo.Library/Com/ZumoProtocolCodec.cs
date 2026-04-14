using System.Globalization;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public sealed class ZumoProtocolCodec : IProtocolCodec
{
    private const byte PingAddress = 0xD1;
    private const string PingPayload = "Ping";

    public bool TryParseLine(string rawLine, out ProtocolFrame frame, out string? error)
    {
        frame = default;
        error = null;

        if (string.IsNullOrWhiteSpace(rawLine))
        {
            error = "Line is empty.";
            return false;
        }

        string normalized = rawLine.TrimEnd('\r', '\n');
        if (normalized.Length < 4)
        {
            error = "Line is too short.";
            return false;
        }

        if (normalized[0] != '5')
        {
            error = "Invalid protocol prefix.";
            return false;
        }

        bool noOperatorFormat = normalized.Length >= 3 && IsHexChar(normalized[1]) && IsHexChar(normalized[2]);
        if (noOperatorFormat)
        {
            bool hasAddressNoOp = byte.TryParse(normalized.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte noOpAddress);
            if (!hasAddressNoOp)
            {
                error = "Invalid address.";
                return false;
            }

            bool parsedNoOpRemainder = TryParseRemainder(MessageType.SetRequest, normalized.AsSpan(3), out byte noOpSdf, out string noOpPayload);
            if (!parsedNoOpRemainder)
            {
                error = "Invalid payload in no-operator request.";
                return false;
            }

            frame = new ProtocolFrame(noOpAddress, MessageType.SetRequest, noOpSdf, noOpPayload, normalized);
            return true;
        }

        bool hasType = TryParseType(normalized[1], out MessageType type);
        if (!hasType)
        {
            error = "Invalid operator.";
            return false;
        }

        bool hasAddress = byte.TryParse(normalized.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte address);
        if (!hasAddress)
        {
            error = "Invalid address.";
            return false;
        }

        bool parsedRemainder = TryParseRemainder(type, normalized.AsSpan(4), out byte sdf, out string payload);
        if (!parsedRemainder)
        {
            error = "Invalid SDF.";
            return false;
        }

        frame = new ProtocolFrame(address, type, sdf, payload, normalized);
        return true;
    }

    public string FormatLine(ProtocolFrame frame)
    {
        string payload = frame.Payload ?? string.Empty;

        if (frame.Type == MessageType.GetResponse)
        {
            return $"5*{frame.Address:X2}{payload}";
        }

        bool pingWithoutOperator = frame.Type == MessageType.SetRequest
            && frame.Address == PingAddress
            && frame.Sdf == 0
            && string.Equals(payload, PingPayload, StringComparison.Ordinal);

        if (pingWithoutOperator)
        {
            return $"5{frame.Address:X2}{payload}";
        }

        char op = TypeToOperator(frame.Type);

        bool omitSdf = frame.Sdf == 0 && payload.Length > 0 && !IsHexString(payload);
        string sdfPart = omitSdf ? string.Empty : frame.Sdf.ToString("X1", CultureInfo.InvariantCulture);
        string line = $"5{op}{frame.Address:X2}{sdfPart}{payload}";

        return line;
    }

    private static bool TryParseType(char op, out MessageType type)
    {
        bool success = true;
        type = MessageType.SetRequest;

        switch (op)
        {
            case '?':
                type = MessageType.GetRequest;
                break;
            case '*':
                type = MessageType.GetResponse;
                break;
            case '<':
                type = MessageType.SetResponse;
                break;
            case '>':
                type = MessageType.SetRequest;
                break;
            case '!':
                type = MessageType.Event;
                break;
            case ':':
                type = MessageType.OneWay;
                break;
            default:
                success = false;
                break;
        }

        return success;
    }

    private static char TypeToOperator(MessageType type)
    {
        char op;

        switch (type)
        {
            case MessageType.GetRequest:
                op = '?';
                break;
            case MessageType.GetResponse:
                op = '*';
                break;
            case MessageType.SetRequest:
                op = '>';
                break;
            case MessageType.SetResponse:
                op = '<';
                break;
            case MessageType.Event:
                op = '!';
                break;
            case MessageType.OneWay:
                op = ':';
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported message type.");
        }

        return op;
    }

    private static bool IsHexChar(char value)
    {
        return char.IsAsciiHexDigit(value);
    }

    private static bool IsHexString(string value)
    {
        return value.All(IsHexChar);
    }

    private static bool TryParseRemainder(MessageType type, ReadOnlySpan<char> remainder, out byte sdf, out string payload)
    {
        sdf = 0;
        payload = string.Empty;

        if (remainder.Length == 0)
        {
            return true;
        }

        if (type == MessageType.GetResponse)
        {
            payload = remainder.ToString();

            if (IsHexChar(remainder[0]))
            {
                bool parsedGetResponseSdf = byte.TryParse(remainder[..1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sdf);
                if (!parsedGetResponseSdf)
                {
                    return false;
                }
            }

            return true;
        }

        if (!IsHexChar(remainder[0]))
        {
            payload = remainder.ToString();
            return true;
        }

        bool parsedSdf = byte.TryParse(remainder[..1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sdf);
        if (!parsedSdf)
        {
            return false;
        }

        payload = remainder.Length > 1 ? remainder[1..].ToString() : string.Empty;
        return true;
    }
}
