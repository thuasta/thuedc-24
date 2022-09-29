using System;
using System.Collections.Generic;
namespace EdcHost;

public class PacketGetStatusInformationHost : Packet
{
    public enum Status
    {
        Standby = 0,
        InProgress = 1,
        Paused = 2
    };


    public const byte PacketId = 0x05;


    private Status _currentStatus;
    private long _currentTime;
    private int _currentScore;
    private Dot _carPos;
    private int _mileage;
    private int _orderListLength;
    private List<Order> _orderList;

    /// <summary>
    /// Construct a GetSiteInformation packet with fields.
    /// </summary>
    /// <remarks>
    /// Note that orederList represents the order ## remaining on the GUI ##
    /// </remarks>
    public PacketGetStatusInformationHost(GameStateType currentState, long currentTime, int currentScore,
        Dot carPos, int mileage, List<Order> orderList)
    {
        // convert Gamestate to Status
        switch (currentState)
        {
            case GameStateType.Unstarted:
            case GameStateType.Ended:
                this._currentStatus = Status.Standby;
                break;
            case GameStateType.Running:
                this._currentStatus = Status.InProgress;
                break;
            case GameStateType.Paused:
                this._currentStatus = Status.Paused;
                break;
        }
        this._currentTime = currentTime;
        this._currentScore = currentScore;
        this._carPos = carPos;
        this._mileage = mileage;
        this._orderListLength = orderList.Count;
        this._orderList = orderList;
    }

    /// <summary>
    /// Construct a GetSiteInformation packet with a raw byte array.
    /// </summary>
    /// <param name="bytes">The raw byte array</param>
    /// <exception cref="ArgumentException">
    /// The raw byte array violates the rules.
    /// </exception>
    public PacketGetStatusInformationHost(byte[] bytes)
    {
        // Validate the packet and extract data
        byte[] data = Packet.ExtractPacketData(bytes);

        byte packetId = bytes[0];
        if (packetId != PacketGetStatusInformationHost.PacketId)
        {
            throw new Exception("The packet ID is incorrect.");
        }
        int currentIndex = 0;
        // status
        this._currentStatus = (Status)data[currentIndex];
        currentIndex += 1;
        // time
        this._currentTime = BitConverter.ToInt64(data, currentIndex);
        currentIndex += 8;
        // score
        this._currentScore = BitConverter.ToInt32(data, currentIndex);
        currentIndex += 4;
        // car
        this._carPos.X = BitConverter.ToInt32(data, currentIndex);
        currentIndex += 4;
        this._carPos.Y = BitConverter.ToInt32(data, currentIndex);
        currentIndex += 4;
        // mileage
        this._mileage = BitConverter.ToInt32(data, currentIndex);
        currentIndex += 4;

        // orderList
        this._orderListLength = BitConverter.ToInt32(data, currentIndex);
        currentIndex += 4;

        //Note that only according to bytes, the _orderList is probably incomplete (with regard to variable 'generationTime' and 'StatusType')
        for (int i = 0; i < this._orderListLength; i++)
        {
            Dot departurePosition = new Dot(BitConverter.ToInt32(data, currentIndex), BitConverter.ToInt32(data, currentIndex + 4));
            currentIndex += 4 * 2;
            Dot destinationPosition = new Dot(BitConverter.ToInt32(data, currentIndex), BitConverter.ToInt32(data, currentIndex + 4));
            currentIndex += 4 * 2;

            long deliveryTimeLimit = BitConverter.ToInt64(data, currentIndex);
            currentIndex += 8;

            // Not used because there's no interface in the constructor of class Order
            bool isTaken = BitConverter.ToBoolean(data, currentIndex);
            currentIndex += 1;

            long generationTime = 0;

            this._orderList.Add(new Order(departurePosition, destinationPosition, generationTime, deliveryTimeLimit));
        }
    }

    public override byte[] GetBytes()
    {
        // Compute the length of the data
        int dataLength = (
            1 +                                    // this._currentStatus
            8 +                                    // this._currentTime
            4 +                                    // this._score
            2 * 4 +                                // this._CarPos
            4 +                                    // this._mileages
            4 +                                    // this._orderListLength
            this._orderListLength * 25             // this._orderList
        );
        // Initialize the data array
        var data = new byte[dataLength];

        int currentIndex = 0;

        data[currentIndex] = (byte)this._currentStatus;
        currentIndex += 1;

        // time
        BitConverter.GetBytes(this._currentTime).CopyTo(data, currentIndex);
        currentIndex += 8;

        // score
        BitConverter.GetBytes(this._currentScore).CopyTo(data, currentIndex);
        currentIndex += 4;

        // carPos
        BitConverter.GetBytes(this._carPos.X).CopyTo(data, currentIndex);
        currentIndex += 4;
        BitConverter.GetBytes(this._carPos.Y).CopyTo(data, currentIndex);
        currentIndex += 4;

        // mileage
        BitConverter.GetBytes(this._mileage).CopyTo(data, currentIndex);
        currentIndex += 4;

        // orderList length 
        BitConverter.GetBytes(this._orderListLength).CopyTo(data, currentIndex);
        currentIndex += 4;

        // orderList
        foreach (Order order in this._orderList)
        {
            // Departure Position
            BitConverter.GetBytes(order.DeparturePosition.X).CopyTo(data, currentIndex);
            currentIndex += 4;
            BitConverter.GetBytes(order.DeparturePosition.Y).CopyTo(data, currentIndex);
            currentIndex += 4;

            // Destination Position
            BitConverter.GetBytes(order.DestinationPosition.X).CopyTo(data, currentIndex);
            currentIndex += 4;
            BitConverter.GetBytes(order.DestinationPosition.Y).CopyTo(data, currentIndex);
            currentIndex += 4;

            // Scheduled time
            BitConverter.GetBytes((long)order.ScheduledDeliveryTime).CopyTo(data, currentIndex);
            currentIndex += 8;

            // isTaken is based on 'order.Status'
            bool isTaken = false;
            switch (order.Status)
            {
                case OrderStatusType.Pending:
                case OrderStatusType.Ungenerated:
                    isTaken = false;
                    break;
                case OrderStatusType.InDelivery:
                    isTaken = true;
                    break;
                // Error: OrderStatusType.Delivered
                default:
                    throw new Exception("Input delivered orders to the PacketGetStatusInformationHost!");
            }
            BitConverter.GetBytes(isTaken).CopyTo(data, currentIndex);
            currentIndex += 1;
        }
        // --------- Finish encoding the 'data' ---------- //

        var header = Packet.GeneratePacketHeader(PacketGetStatusInformationHost.PacketId, data);

        var bytes = new byte[header.Length + data.Length];
        header.CopyTo(bytes, 0);
        data.CopyTo(bytes, header.Length);

        return bytes;
    }
}