using System;

namespace EdcHost;

/// <summary>
/// A packet for slaves to set a charging pile
/// </summary>
public class PacketSetChargingPileSlave : Packet
{
    public const byte PacketId = 0x02;

    /// <summary>
    /// Construct a SetChargingPileSlave packet with fields.
    /// </summary>
    public PacketSetChargingPileSlave()
    {
        // Empty
    }

    /// <summary>
    /// Construct a SetChargingPileSlave packet with a raw
    /// byte array.
    /// </summary>
    /// <param name="bytes">The raw byte array</param>
    /// <exception cref="Exception">
    /// The raw byte array violates the rules.
    /// </exception>
    public PacketSetChargingPileSlave(byte[] bytes)
    {
        // Validate the packet and extract data
        Packet.ExtractPacketData(bytes);

        byte packetId = bytes[0];
        if (packetId != PacketSetChargingPileSlave.PacketId)
        {
            throw new Exception("The packet ID is incorrect.");
        }
    }

    public override byte[] GetBytes()
    {
        var data = new byte[8];

        var header = Packet.GeneratePacketHeader(PacketSetChargingPileSlave.PacketId, data);

        var bytes = new byte[header.Length + data.Length];
        header.CopyTo(bytes, 0);
        data.CopyTo(bytes, header.Length);

        return bytes;
    }
}