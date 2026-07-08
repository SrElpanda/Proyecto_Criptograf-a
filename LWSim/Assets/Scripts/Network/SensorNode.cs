using UnityEngine;

public class SensorNode : MonoBehaviour
{
    [Header("References")]
    public TemperatureField temperatureField;
    public HeatmapRenderer heatmapRenderer;
    public CentralHub hub;

    [Header("Messaging")]
    public float sendInterval = 2f;

    [Header("State")]
    public float lastTemperature;

    private Renderer rend;
    private float sendTimer;
    private string sensorId;
    private IMessageStrategy strategy = new PlainStrategy();

    public static bool SendingPaused { get; set; }

    public string SensorId => sensorId;

    void Start()
    {
        rend = GetComponent<Renderer>();

        sensorId = "sensor_" + gameObject.GetEntityId();
        sendTimer = sendInterval;

        UpdateVisual();
    }

    void Update()
    {
        if (SendingPaused)
            return;

        sendTimer -= Time.deltaTime;

        if (sendTimer <= 0f)
        {
            sendTimer = sendInterval;
            SendMessageToHub();
        }
    }

    void SendMessageToHub()
    {
        if (hub == null)
            return;

        Vector2 pos = new Vector2(
            transform.position.x,
            transform.position.z
        );

        float temp =
            heatmapRenderer.GetTemperatureAtWorldPosition(pos);

        lastTemperature = temp;

        SensorMessage msg =
            new SensorMessage(sensorId, temp);

        MessagePacket packet =
            MessagePacket.Wrap(msg, strategy);

        hub.ReceivePacket(packet);
    }

    public void UpdateVisual()
    {
        Vector2 pos = new Vector2(
            transform.position.x,
            transform.position.z
        );

        float temp =
            heatmapRenderer.GetTemperatureAtWorldPosition(pos);

        float normalized =
            Mathf.InverseLerp(
                temperatureField.averageTemperature - temperatureField.variation,
                temperatureField.averageTemperature + temperatureField.variation,
                temp
            );

        Color color =
            Color.Lerp(
                Color.blue,
                Color.red,
                normalized
            );

        rend.material.color = color;
    }
}
