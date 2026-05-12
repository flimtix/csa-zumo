using System.Net;

namespace Zumo.RemoteControl.Utils;

public static class IPAddressUtils
{
    public static IPAddress ResolveBindAddress(string? bindAddress)
    {
        IPAddress address;
        if (string.IsNullOrWhiteSpace(bindAddress) || bindAddress == "0.0.0.0")
        {
            address = IPAddress.Any;
        }
        else if (IPAddress.TryParse(bindAddress, out IPAddress? parsedAddress))
        {
            address = parsedAddress;
        }
        else
        {
            address = IPAddress.Any;
        }
        return address;
    }
}