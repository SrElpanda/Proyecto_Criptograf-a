using System;

[Serializable]
public class MessagePacket
{
    public string strategyName;
    public int sequenceNumber;
    public string serializedData;

    // Transient — no se serializa, se llena al unwrap
    [NonSerialized] public SensorMessage message;

    private static int _globalSequence = 0;

    public MessagePacket() { }

    public static MessagePacket Wrap(
        SensorMessage msg,
        IMessageStrategy strategy)
    {
        var packet = new MessagePacket
        {
            strategyName = strategy.GetType().Name,
            sequenceNumber = _globalSequence++,
            serializedData = strategy.Serialize(msg),
            message = msg
        };
        return packet;
    }

    public SensorMessage Unwrap(IMessageStrategy strategy)
    {
        message = strategy.Deserialize(serializedData);
        return message;
    }
}
