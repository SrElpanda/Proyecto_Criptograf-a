public interface IMessageStrategy
{
    string Serialize(SensorMessage message);
    SensorMessage Deserialize(string data);
}
