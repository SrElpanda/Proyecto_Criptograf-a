using UnityEngine;

public class SensorSpawner : MonoBehaviour
{
    public TemperatureField temperatureField;
    public HeatmapRenderer heatmapRenderer;

    public GameObject sensorPrefab;

    public int sensorCount = 16;

    public float radius = 15f;

    private CentralHub hub;

    void Start()
    {
        hub = FindAnyObjectByType<CentralHub>();
        SpawnSensors();
    }

    void SpawnSensors()
    {
        for (int i = 0; i < sensorCount; i++)
        {
            float angle =
                i * Mathf.PI * 2f / sensorCount;

            float x =
                Mathf.Cos(angle) * radius;

            float z =
                Mathf.Sin(angle) * radius;

            GameObject sensor = Instantiate(
                sensorPrefab,
                new Vector3(x, 0.5f, z),
                Quaternion.identity
            );

            SensorNode node =
                sensor.GetComponent<SensorNode>();

            node.temperatureField =
                temperatureField;

            node.heatmapRenderer =
                heatmapRenderer;

            node.hub =
                hub;
        }
    }
}
