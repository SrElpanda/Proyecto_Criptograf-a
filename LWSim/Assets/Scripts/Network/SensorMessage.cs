using System;

[Serializable]
public class SensorMessage
{
    public string sensorId;
    public float temperature;
    public string timestamp;

    public SensorMessage() { }

    public SensorMessage(string sensorId, float temperature)
    {
        this.sensorId = sensorId;
        this.temperature = temperature;
        this.timestamp = DateTime.UtcNow.ToString("o");
    }
}
