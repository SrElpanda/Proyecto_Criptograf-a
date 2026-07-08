using UnityEngine;

public class PlainStrategy : IMessageStrategy
{
    public string Serialize(SensorMessage message)
    {
        return JsonUtility.ToJson(message);
    }

    public SensorMessage Deserialize(string data)
    {
        return JsonUtility.FromJson<SensorMessage>(data);
    }
}
