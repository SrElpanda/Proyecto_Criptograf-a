using UnityEngine;

public class HeatmapRenderer : MonoBehaviour
{
    public TemperatureField temperatureField;

    public Renderer targetRenderer;

    public int textureResolution = 128;

    float minTemperature =>
        temperatureField.averageTemperature - temperatureField.variation;
    float maxTemperature =>
        temperatureField.averageTemperature + temperatureField.variation;

    private Texture2D heatTexture;

    void Start()
    {
        temperatureField.GenerateSensors();
        GenerateHeatmap();
    }

    void GenerateHeatmap()
    {
        heatTexture = new Texture2D(
            textureResolution,
            textureResolution
        );

        heatTexture.filterMode = FilterMode.Bilinear;
        heatTexture.wrapMode = TextureWrapMode.Clamp;

        float planeSize = temperatureField.planeSize;

        for (int x = 0; x < textureResolution; x++)
        {
            for (int y = 0; y < textureResolution; y++)
            {
                float normalizedX =
                    (float)x / (textureResolution - 1);

                float normalizedY =
                    (float)y / (textureResolution - 1);

                float worldX =
                    normalizedX * planeSize - planeSize / 2f;

                float worldY =
                    normalizedY * planeSize - planeSize / 2f;

                float temp =
                    temperatureField.GetTemperature(
                        new Vector2(worldX, worldY)
                    );

                float normalizedTemp =
                    Mathf.InverseLerp(
                        minTemperature,
                        maxTemperature,
                        temp
                    );

                normalizedTemp = Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTemp
                );

                Color color =
                    Color.Lerp(
                        Color.blue,
                        Color.red,
                        normalizedTemp
                    );

                heatTexture.SetPixel(x, y, color);
            }
        }

        heatTexture.Apply();
        targetRenderer.material.SetTexture("_BaseMap", heatTexture);
    }

    public float GetTemperatureAtWorldPosition(
        Vector2 worldPos
    )
    {
        float planeSize = temperatureField.planeSize;

        float normalizedX =
            (worldPos.x + planeSize / 2f) / planeSize;

        float normalizedY =
            (worldPos.y + planeSize / 2f) / planeSize;

        int pixelX =
            Mathf.RoundToInt(
                normalizedX * (textureResolution - 1)
            );

        int pixelY =
            Mathf.RoundToInt(
                normalizedY * (textureResolution - 1)
            );

        Color color =
            heatTexture.GetPixel(pixelX, pixelY);

        float t = color.r;

        return Mathf.Lerp(
            minTemperature,
            maxTemperature,
            t
        );
    }
}