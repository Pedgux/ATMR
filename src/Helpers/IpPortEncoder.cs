namespace ATMR.Networking;

public static class IpPortEncoder
{
    // Encode IP + port to tiny Base64 string
    public static string Encode(string ip, ushort port)
    {
        // Split IP into 4 bytes
        string[] parts = ip.Split('.');
        if (parts.Length != 4)
            throw new ArgumentException("Invalid IPv4 address");

        byte[] bytes = new byte[6];
        for (int i = 0; i < 4; i++)
            bytes[i] = byte.Parse(parts[i]);

        // Port → 2 bytes (big-endian)
        bytes[4] = (byte)(port >> 8);
        bytes[5] = (byte)(port & 0xFF);

        // Convert to Base64
        return Convert.ToBase64String(bytes);
    }

    // Decode Base64 blob back to IP + port
    public static (string ip, ushort port) Decode(string blob)
    {
        byte[] bytes = Convert.FromBase64String(blob);
        if (bytes.Length != 6)
            throw new ArgumentException("Invalid blob");

        string ip = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        ushort port = (ushort)((bytes[4] << 8) + bytes[5]);

        return (ip, port);
    }
}
