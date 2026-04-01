using System.Globalization;

namespace Zumo.Library.Com.Protocol;

internal static class HexEncoding
{
    public static string UInt16(ushort value)
    {
        return value.ToString("X4", CultureInfo.InvariantCulture);
    }

    public static string Int16(short value)
    {
        return unchecked((ushort)value).ToString("X4", CultureInfo.InvariantCulture);
    }

    public static string Byte(byte value)
    {
        return value.ToString("X2", CultureInfo.InvariantCulture);
    }

    public static short ParseInt16(string hex)
    {
        ushort unsignedValue = ParseUInt16(hex);
        return unchecked((short)unsignedValue);
    }

    public static ushort ParseUInt16(string hex)
    {
        return ushort.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
