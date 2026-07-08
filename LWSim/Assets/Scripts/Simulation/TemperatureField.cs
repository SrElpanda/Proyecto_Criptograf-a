using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SensorPoint
{
    public Vector2 position;
    public float temperature;
}

public class TemperatureField : MonoBehaviour
{
    [Header("Grid")]
    public int gridSize = 4;
    public float planeSize = 10f;

    [Header("Temperature")]
    public float averageTemperature = 25f;
    public float variation = 5f;

    public List<SensorPoint> sensors = new List<SensorPoint>();

    void Start()
    {
        GenerateSensors();
    }

    public void GenerateSensors()
    {
        sensors.Clear();

        float spacing = planeSize / (gridSize - 1);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                SensorPoint sensor = new SensorPoint();

                sensor.position = new Vector2(
                    x * spacing - planeSize / 2f,
                    y * spacing - planeSize / 2f
                );

                sensor.temperature =
                    averageTemperature +
                    Random.Range(-variation, variation);

                sensors.Add(sensor);
            }
        }
    }

    public float GetTemperature(Vector2 position)
    {
        float weightedSum = 0f;
        float weightTotal = 0f;

        foreach (var sensor in sensors)
        {
            float distance =
                Vector2.Distance(position, sensor.position);

            if (distance < 0.01f)
                return sensor.temperature;

            float weight = Mathf.Exp(-distance * 0.2f);

            weightedSum += sensor.temperature * weight;
            weightTotal += weight;
        }

        if (weightTotal <= 0f)
        {
            Debug.LogError("Weight total is zero");
            return averageTemperature;
        }

        return weightedSum / weightTotal;
    }

    void OnDrawGizmos()
    {
        if (sensors == null)
            return;

        foreach (var sensor in sensors)
        {
            float normalized =
                Mathf.InverseLerp(
                    averageTemperature - variation,
                    averageTemperature + variation,
                    sensor.temperature
                );

            Gizmos.color =
                Color.Lerp(Color.blue, Color.red, normalized);

            Vector3 pos = new Vector3(
                sensor.position.x,
                0.2f,
                sensor.position.y
            );

            Gizmos.DrawSphere(pos, 0.3f);
        }
    }
}